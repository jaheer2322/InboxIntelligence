using AutoMapper;
using InboxIntelligence.Models;
using InboxIntelligence.Models.DTOs;
using InboxIntelligence.Repositories;
using InboxIntelligence.Services;
using Moq;

namespace InboxIntelligence.Tests
{
    [TestClass]
    public class EmailServiceTests
    {
        private EmailService _emailService = null!;
        private Mock<IEmailRepository> _emailRepoMock = null!;
        private Mock<IMapper> _mapperMock = null!;

        [TestInitialize]
        public void Setup()
        {
            _emailRepoMock = new Mock<IEmailRepository>();
            _mapperMock = new Mock<IMapper>();

            _emailService = new EmailService(_emailRepoMock.Object, _mapperMock.Object);
        }

        [TestMethod]
        public async Task ImportEmailsAsync_WhenAllEmailsExist_ReturnsZero()
        {
            // Arrange
            var emails = new List<BulkEmailImportDTO> { new ()
                {
                    ExternalMessageId = "email-001",
                    ConversationId = "conversation-001",
                    Subject = "Project kickoff",
                    Sender = "alice@example.com",
                    Recipients = ["jaheer@example.com"],
                    Body = "Project kickoff meeting is on Monday.",
                    Timestamp = DateTime.Parse("2026-10-12T10:00:00Z")
                } 
            };

            _emailRepoMock.Setup(repo => repo.GetExistingMessageIdsAsync(It.IsAny<IEnumerable<string>>()))
                      .ReturnsAsync(new HashSet<string> { "email-001" });

            // Act
            var result = await _emailService.ImportEmailsAsync(emails);

            // Assert
            Assert.AreEqual(0, result);

            _emailRepoMock.Verify(
                repo => repo.AddRangeAsync(It.IsAny<IEnumerable<Email>>()),
                Times.Never               
                );
        }

        [TestMethod]
        public async Task ImportEmailsAsync_WhenEmailIsNew_ReturnsOne()
        {
            // Arrange
            var emails = new List<BulkEmailImportDTO>() {
                new(){
                    ExternalMessageId = "email-new-001",
                    ConversationId = "conversation-002",
                    Subject = "New project update",
                    Sender = "alice@example.com",
                    Recipients = ["jaheer@example.com"],
                    Body = "The project update is ready.",
                    Timestamp = DateTime.Parse("2026-10-12T10:00:00Z")
                }
            };

            _emailRepoMock.Setup(repo => repo.GetExistingMessageIdsAsync(It.IsAny<IEnumerable<string>>()))
                      .ReturnsAsync(new HashSet<string>());
            _mapperMock.Setup(mapper => mapper.Map<List<Email>>(It.IsAny<IEnumerable<BulkEmailImportDTO>>()))
                      .Returns((IEnumerable<BulkEmailImportDTO> source) => 
                            source.Select(dto => new Email {
                                ExternalMessageId = dto.ExternalMessageId,
                                ConversationId = dto.ConversationId,
                                Subject = dto.Subject,
                                Sender = dto.Sender,
                                Recipients = string.Join(", ", dto.Recipients),
                                Body = dto.Body,
                                SentAt = dto.Timestamp
                            }).ToList());

            // Act
            var result = await _emailService.ImportEmailsAsync(emails);

            // Assert
            Assert.AreEqual(1, result);

            _emailRepoMock.Verify(
                mock => mock.AddRangeAsync(
                    It.Is<IEnumerable<Email>>(
                        em => em.Count() == 1 && em.First().ExternalMessageId == "email-new-001")),
                Times.Once);
        }


        [TestMethod]
        public async Task ImportEmailsAsync_WhenBatchContainsDuplicateIds_InsertsOnlyOne()
        {
            // Arrange
            var email = new BulkEmailImportDTO
            {
                ExternalMessageId = "email-003",
                ConversationId = "conversation-003",
                Subject = "Project update",
                Sender = "alice@example.com",
                Recipients = ["jaheer@example.com"],
                Body = "Project update.",
                Timestamp = DateTime.Parse("2026-10-12T10:00:00Z")
            };

            var emails = new List<BulkEmailImportDTO>
            {
                email,
                email
            };

            _emailRepoMock
                .Setup(repo => repo.GetExistingMessageIdsAsync(
                    It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(new HashSet<string>());

            _mapperMock
                .Setup(mapper => mapper.Map<List<Email>>(
                    It.IsAny<IEnumerable<BulkEmailImportDTO>>()))
                .Returns((IEnumerable<BulkEmailImportDTO> source) =>
                    source.Select(dto => new Email
                    {
                        ExternalMessageId = dto.ExternalMessageId,
                        ConversationId = dto.ConversationId,
                        Subject = dto.Subject,
                        Sender = dto.Sender,
                        Recipients = string.Join(", ", dto.Recipients),
                        Body = dto.Body,
                        SentAt = dto.Timestamp
                    }).ToList());

            // Act
            var result = await _emailService.ImportEmailsAsync(emails);

            // Assert
            Assert.AreEqual(1, result);

            _emailRepoMock.Verify(
                repo => repo.AddRangeAsync(
                    It.Is<IEnumerable<Email>>(
                        inserted => inserted.Count() == 1)),
                Times.Once);
        }


        [TestMethod]
        public async Task ImportEmailsAsync_WhenExternalMessageIdIsEmpty_ThrowsBadRequestException()
        {
            // Arrange
            var emails = new List<BulkEmailImportDTO>
            {
                new()
                {
                    ExternalMessageId = "",
                    Subject = "Invalid email"
                }
            };

            // Act & Assert
            await Assert.ThrowsExactlyAsync<InboxIntelligence.Exceptions.BadRequestException>(
                () => _emailService.ImportEmailsAsync(emails));

            _emailRepoMock.Verify(
                repo => repo.GetExistingMessageIdsAsync(
                    It.IsAny<IEnumerable<string>>()),
                Times.Never);

            _emailRepoMock.Verify(
                repo => repo.AddRangeAsync(
                    It.IsAny<IEnumerable<InboxIntelligence.Models.Email>>()),
                Times.Never);
        }


        [TestMethod]
        public async Task ImportEmailsAsync_WhenBatchIsEmpty_ReturnsZero()
        {
            // Arrange
            var emails = new List<BulkEmailImportDTO>();

            // Act
            var result = await _emailService.ImportEmailsAsync(emails);

            // Assert
            Assert.AreEqual(0, result);

            _emailRepoMock.Verify(
                repo => repo.GetExistingMessageIdsAsync(
                    It.IsAny<IEnumerable<string>>()),
                Times.Never);

            _emailRepoMock.Verify(
                repo => repo.AddRangeAsync(
                    It.IsAny<IEnumerable<InboxIntelligence.Models.Email>>()),
                Times.Never);
        }


    }
}
