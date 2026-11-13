/* RaceDay front-end behaviour. No frameworks beyond Bootstrap's modal/collapse. */
(function () {
  "use strict";

  var $ = function (sel, root) { return (root || document).querySelector(sel); };
  var $$ = function (sel, root) { return Array.prototype.slice.call((root || document).querySelectorAll(sel)); };

  /* ---------------------------------------------------------------- Alerts */
  $$(".rd-alert__close").forEach(function (btn) {
    btn.addEventListener("click", function () {
      var alert = btn.closest(".rd-alert");
      if (alert) { alert.remove(); }
    });
  });
  // Success messages fade away on their own; errors stay until dismissed.
  $$(".rd-alert--success").forEach(function (el) {
    setTimeout(function () { el.remove(); }, 6000);
  });

  /* ------------------------------------------------ Event list live filters */
  $$("[data-event-filters]").forEach(function (root) {
    var cards = $$("[data-event-card]", root);
    var grid = $("[data-event-grid]", root);
    var emptyState = $("[data-filter-empty]", root);
    var countEl = $("[data-filter-count]", root);
    var labelEl = $("[data-filter-label]", root);
    var f = {
      search: $('[data-filter="search"]', root),
      type: $('[data-filter="type"]', root),
      distance: $('[data-filter="distance"]', root),
      sort: $('[data-filter="sort"]', root),
      past: $('[data-filter="past"]', root)
    };

    function apply() {
      var q = (f.search.value || "").trim().toLowerCase();
      var type = f.type.value;
      var range = f.distance.value ? f.distance.value.split("-").map(parseFloat) : null;
      var showPast = f.past.checked;
      var visible = [];

      cards.forEach(function (card) {
        var d = parseFloat(card.dataset.distance);
        var ok = true;
        if (q && card.dataset.name.indexOf(q) === -1 && card.dataset.location.indexOf(q) === -1) { ok = false; }
        if (type && card.dataset.type !== type) { ok = false; }
        if (range && (d < range[0] || d > range[1])) { ok = false; }
        if (!showPast && card.dataset.past === "1") { ok = false; }
        card.hidden = !ok;
        if (ok) { visible.push(card); }
      });

      var sorters = {
        date: function (a, b) { return a.dataset.date.localeCompare(b.dataset.date); },
        distance: function (a, b) { return parseFloat(a.dataset.distance) - parseFloat(b.dataset.distance); },
        name: function (a, b) { return a.dataset.name.localeCompare(b.dataset.name); }
      };
      cards.slice().sort(sorters[f.sort.value] || sorters.date).forEach(function (c) { grid.appendChild(c); });

      countEl.textContent = visible.length;
      var anyFilter = q || type || range || showPast;
      labelEl.textContent = (visible.length === 1 ? "event" : "events") + (anyFilter ? " match your filters" : (showPast ? "" : " upcoming"));
      if (emptyState) { emptyState.hidden = visible.length !== 0 || cards.length === 0; }
    }

    Object.keys(f).forEach(function (k) {
      f[k].addEventListener(k === "search" ? "input" : "change", apply);
    });
    $$("[data-filter-reset]", root).forEach(function (btn) {
      btn.addEventListener("click", function () {
        f.search.value = ""; f.type.value = ""; f.distance.value = ""; f.sort.value = "date"; f.past.checked = false;
        apply();
      });
    });
    apply();
  });

  /* --------------------------------------------- Confirmation modal (forms) */
  var modalEl = $("#confirmModal");
  if (modalEl && window.bootstrap) {
    var modal = new bootstrap.Modal(modalEl);
    var pending = null;

    function ask(title, message, buttonText, onAccept) {
      $("#confirmTitle").textContent = title || "Are you sure?";
      $("#confirmMessage").textContent = message || "";
      $("#confirmAccept").textContent = buttonText || "Yes, continue";
      pending = onAccept;
      modal.show();
    }

    $("#confirmAccept").addEventListener("click", function () {
      modal.hide();
      if (pending) { var fn = pending; pending = null; fn(); }
    });

    $$("form.js-confirm").forEach(function (form) {
      form.addEventListener("submit", function (e) {
        if (form.dataset.confirmed === "1") { return; }
        e.preventDefault();
        ask(form.dataset.confirmTitle, form.dataset.confirmMessage, form.dataset.confirmButton, function () {
          form.dataset.confirmed = "1";
          form.submit();
        });
      });
    });

    // Buttons with their own formaction (e.g. "Remove result" inside the results form)
    $$("button[data-confirm-title]").forEach(function (btn) {
      btn.addEventListener("click", function (e) {
        e.preventDefault();
        var form = btn.form;
        ask(btn.dataset.confirmTitle, btn.dataset.confirmMessage, btn.dataset.confirmButton, function () {
          var hidden = document.createElement("input");
          hidden.type = "hidden"; hidden.name = "__skip"; hidden.value = "1";
          form.appendChild(hidden);
          form.action = btn.getAttribute("formaction");
          form.submit();
        });
      });
    });
  }

  /* --------------------------------------- Form helpers: validation, submit */
  $$("form[data-validate]").forEach(function (form) {
    form.addEventListener("submit", function (e) {
      // Passwords must match before we bother the server.
      $$("[data-match]", form).forEach(function (input) {
        var other = $(input.dataset.match, form);
        input.setCustomValidity(other && other.value !== input.value ? "The two passwords do not match." : "");
      });

      if (!form.checkValidity()) {
        e.preventDefault();
        e.stopImmediatePropagation();
        form.classList.add("was-validated");
        var firstBad = $(":invalid", form);
        if (firstBad) { firstBad.focus(); }
        return;
      }
    });
    $$("[data-match]", form).forEach(function (input) {
      input.addEventListener("input", function () { input.setCustomValidity(""); });
    });
  });

  // Stop double submits and show progress on the clicked button.
  $$("form").forEach(function (form) {
    form.addEventListener("submit", function (e) {
      if (e.defaultPrevented) { return; }
      var btn = e.submitter || $('button[type="submit"][data-submit-label]', form);
      if (btn && btn.dataset.submitLabel) {
        // Defer so the form's own data is posted before the button is disabled.
        setTimeout(function () {
          btn.disabled = true;
          btn.classList.add("is-loading");
          btn.textContent = btn.dataset.submitLabel;
        }, 0);
      }
    });
  });

  // bfcache: re-enable buttons when coming back with the Back button
  window.addEventListener("pageshow", function (e) {
    if (e.persisted) {
      $$("button.is-loading").forEach(function (b) { b.disabled = false; b.classList.remove("is-loading"); });
    }
  });

  /* ------------------------------------------------------- Password helpers */
  $$("[data-toggle-password]").forEach(function (btn) {
    btn.addEventListener("click", function () {
      var input = btn.parentElement.querySelector("input");
      var show = input.type === "password";
      input.type = show ? "text" : "password";
      btn.textContent = show ? "Hide" : "Show";
      btn.setAttribute("aria-label", show ? "Hide password" : "Show password");
    });
  });

  $$("[data-strength-target]").forEach(function (input) {
    var meter = $(input.dataset.strengthTarget);
    if (!meter) { return; }
    var bar = $(".strength__bar", meter);
    var text = $(".strength__text", meter);
    input.addEventListener("input", function () {
      var v = input.value, score = 0;
      if (v.length >= 8) { score++; }
      if (/[A-Z]/.test(v) && /[a-z]/.test(v)) { score++; }
      if (/\d/.test(v)) { score++; }
      if (/[^A-Za-z0-9]/.test(v)) { score++; }
      var levels = [
        ["0%", "var(--line)", "At least 8 characters"],
        ["25%", "var(--bad-dot)", "Weak"],
        ["50%", "var(--wait-dot)", "Fair"],
        ["75%", "var(--lime-dark)", "Good"],
        ["100%", "var(--ok-dot)", "Strong"]
      ];
      var l = levels[v.length === 0 ? 0 : Math.max(score, 1)];
      bar.style.setProperty("--w", l[0]);
      bar.style.setProperty("--c", l[1]);
      text.textContent = l[2];
    });
  });

  /* --------------------------------------------------- Image file choosers */
  $$("[data-image-input]").forEach(function (input) {
    var preview = $(input.dataset.preview);
    var hide = input.dataset.hide ? $(input.dataset.hide) : null;
    var note = input.closest("form") ? $("[data-file-note]", input.closest("form")) : null;
    var maxBytes = (parseFloat(input.dataset.maxMb) || 5) * 1024 * 1024;
    var original = note ? note.textContent : "";
    var submit = $$('[data-needs-file]', input.closest("form")).filter(function (b) { return b.dataset.needsFile === "#" + input.id; })[0];

    input.addEventListener("change", function () {
      var file = input.files && input.files[0];
      if (note) { note.classList.remove("is-error"); note.textContent = original; }
      if (submit) { submit.disabled = true; }
      if (!file) { return; }

      var okType = /^image\/(png|jpe?g|webp|gif)$/.test(file.type);
      if (!okType || file.size > maxBytes) {
        input.value = "";
        if (note) {
          note.classList.add("is-error");
          note.textContent = !okType ? "Only JPG, PNG, WEBP or GIF images are allowed." : "That image is larger than " + (maxBytes / 1048576) + " MB.";
        }
        return;
      }

      if (note) { note.textContent = file.name; }
      if (submit) { submit.disabled = false; }
      if (preview) {
        var reader = new FileReader();
        reader.onload = function (ev) {
          preview.src = ev.target.result;
          preview.hidden = false;
          if (hide) { hide.hidden = true; }
        };
        reader.readAsDataURL(file);
      }
    });
  });

  /* -------------------------------------------------- Misc small enhancers */
  $$("select[data-autosubmit]").forEach(function (sel) {
    sel.addEventListener("change", function () { sel.form.submit(); });
  });

  $$("[data-counter]").forEach(function (area) {
    var out = $(area.dataset.counter);
    var update = function () { if (out) { out.textContent = area.value.length; } };
    area.addEventListener("input", update);
    update();
  });

  // Finish times: keep digits and colons only, add colons as you type (e.g. 13520 -> 1:35:20)
  $$(".time-input").forEach(function (input) {
    input.addEventListener("input", function () {
      var raw = input.value;
      if (raw.indexOf(":") !== -1) {
        input.value = raw.replace(/[^\d:]/g, "").slice(0, 8);
        return;
      }
      var digits = raw.replace(/\D/g, "").slice(0, 6);
      if (digits.length > 4) { digits = digits.slice(0, digits.length - 4) + ":" + digits.slice(-4, -2) + ":" + digits.slice(-2); }
      else if (digits.length > 2) { digits = digits.slice(0, digits.length - 2) + ":" + digits.slice(-2); }
      input.value = digits;
    });
  });

  // Results page: give positions 1..n in order of finish time
  var rank = $("#autoRank");
  if (rank) {
    var toSeconds = function (text) {
      var p = (text || "").split(":").map(function (x) { return parseInt(x, 10); });
      if (p.some(isNaN)) { return null; }
      return p.length === 3 ? p[0] * 3600 + p[1] * 60 + p[2] : p.length === 2 ? p[0] * 60 + p[1] : null;
    };
    rank.addEventListener("click", function () {
      var rows = $$("#resultsForm tbody tr").map(function (tr) {
        return { time: toSeconds($(".time-input", tr).value), pos: $(".position-input", tr) };
      }).filter(function (r) { return r.time !== null && r.time > 0; });
      rows.sort(function (a, b) { return a.time - b.time; });
      rows.forEach(function (r, i) { r.pos.value = i + 1; });
      rank.textContent = rows.length ? "Ranked " + rows.length + " finisher" + (rows.length === 1 ? "" : "s") : "Enter some times first";
      setTimeout(function () { rank.textContent = "Rank by finish time"; }, 2500);
    });
  }

  // Category form: reuse the add form for editing
  var catForm = $("#categoryForm");
  if (catForm) {
    var heading = $("#categoryHeading"), submitBtn = $("#categorySubmit"), resetBtn = $("#categoryReset");
    var defaults = { distance: $("#catDistance").value };
    $$("[data-edit-category]").forEach(function (btn) {
      btn.addEventListener("click", function () {
        $("#CategoryId").value = btn.dataset.id;
        $("#catName").value = btn.dataset.name;
        $("#catMin").value = btn.dataset.min;
        $("#catMax").value = btn.dataset.max;
        $("#catDistance").value = btn.dataset.distance;
        heading.textContent = "Edit category";
        submitBtn.textContent = "Save category";
        resetBtn.hidden = false;
        $("#categoryPanel").scrollIntoView({ behavior: "smooth", block: "center" });
        $("#catName").focus();
      });
    });
    resetBtn.addEventListener("click", function () {
      catForm.reset();
      $("#CategoryId").value = 0;
      $("#catDistance").value = defaults.distance;
      heading.textContent = "Add a category";
      submitBtn.textContent = "Add category";
      resetBtn.hidden = true;
    });
    catForm.addEventListener("submit", function (e) {
      var min = parseInt($("#catMin").value, 10), max = parseInt($("#catMax").value, 10);
      if (!isNaN(min) && !isNaN(max) && max < min) {
        e.preventDefault();
        e.stopImmediatePropagation();
        $("#catMax").setCustomValidity("Maximum age must be the same as or higher than minimum age.");
        $("#catMax").reportValidity();
        $("#catMax").addEventListener("input", function () { this.setCustomValidity(""); }, { once: true });
      }
    });
  }

  // Countdown to event day
  $$("[data-countdown]").forEach(function (el) {
    var target = new Date(el.dataset.countdown);
    if (isNaN(target.getTime())) { return; }
    function tick() {
      var diff = target.getTime() - Date.now();
      if (diff <= 0) { el.textContent = "Race day is here"; return; }
      var days = Math.floor(diff / 86400000);
      var hours = Math.floor((diff % 86400000) / 3600000);
      el.textContent = days > 0
        ? days + (days === 1 ? " day" : " days") + " to go"
        : hours + (hours === 1 ? " hour" : " hours") + " to go";
    }
    tick();
    setInterval(tick, 60000);
  });
})();
