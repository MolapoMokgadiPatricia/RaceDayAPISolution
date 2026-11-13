using RaceDayAPI.Services;

namespace RaceDayAPI.Tests
{
    public class PasswordServiceTests
    {
        private readonly PasswordService _sut = new();

        [Fact]
        public void HashPassword_ReturnsDifferentHashFromPlainPassword()
        {
            string password = "RaceDayTest123!";
            Assert.NotEqual(password, _sut.HashPassword(password));
        }

        [Fact]
        public void VerifyPassword_ReturnsTrueForCorrectPassword()
        {
            string password = "RaceDayTest123!";
            string hash = _sut.HashPassword(password);
            Assert.True(_sut.VerifyPassword(password, hash));
        }

        [Fact]
        public void VerifyPassword_ReturnsFalseForIncorrectPassword()
        {
            string hash = _sut.HashPassword("RaceDayTest123!");
            Assert.False(_sut.VerifyPassword("WrongPassword!", hash));
        }

        [Fact]
        public void HashPassword_GeneratesDifferentHashesForSamePassword()
        {
            string password = "RaceDayTest123!";
            Assert.NotEqual(_sut.HashPassword(password), _sut.HashPassword(password));
        }

        [Fact]
        public void VerifyPassword_ReturnsFalseForMalformedHash()
        {
            Assert.False(_sut.VerifyPassword("anything", "not-a-valid-hash"));
        }
    }
}