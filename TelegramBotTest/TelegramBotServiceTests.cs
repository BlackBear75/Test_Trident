using System.Threading;
using System.Threading.Tasks;
using Moq;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBot.API;
using TelegramBot.BusinessLogic;
using TelegramBot.Entity.User;
using TelegramBot.Entity.User.Repository;
using TelegramBot.Entity.PhpScript;
using TelegramBot.Entity.PhpScript.Repository;
using Xunit;
using User = TelegramBot.Entity.User.User;

public class TelegramBotServiceTests
{
    private readonly Mock<ITelegramBotClient> _mockBotClient;
    private readonly Mock<IUserRepository<User?>> _mockUserRepository;
    private readonly Mock<IScriptGeneratorService> _mockScriptGeneratorService;
    private readonly Mock<ISftpService> _mockSftpService;
    private readonly Mock<IPhpScriptRepository<PhpScript?>> _mockPhpScriptRepository;
    private readonly Mock<ITelegramBotClientWrapper> _mockTelegramBotClientWrapper;
    private readonly TelegramBotService _telegramBotService;

    public TelegramBotServiceTests()
    {
        _mockBotClient = new Mock<ITelegramBotClient>();
        _mockUserRepository = new Mock<IUserRepository<User?>>();
        _mockScriptGeneratorService = new Mock<IScriptGeneratorService>();
        _mockSftpService = new Mock<ISftpService>();
        _mockPhpScriptRepository = new Mock<IPhpScriptRepository<PhpScript?>>();
        _mockTelegramBotClientWrapper = new Mock<ITelegramBotClientWrapper>();

        _telegramBotService = new TelegramBotService(
            _mockBotClient.Object,
            null, 
            _mockUserRepository.Object,
            _mockScriptGeneratorService.Object,
            _mockSftpService.Object,
            _mockPhpScriptRepository.Object,
            _mockTelegramBotClientWrapper.Object
        );
    }

    [Fact]
    public async Task UpdateHandler_ShouldCreateUser_WhenUserNotFound()
    {
        // Arrange
        var update = CreateMessageUpdate(123, "testuser", "/start");

        _mockUserRepository.Setup(repo => repo.FindByIdAsync(It.IsAny<long>()))
            .ReturnsAsync((User ?)null);

        _mockUserRepository.Setup(repo => repo.InsertOneAsync(It.IsAny<User>()))
            .Returns(Task.CompletedTask);

        // Act
        await _telegramBotService.UpdateHandler(_mockBotClient.Object, update, CancellationToken.None);

        // Assert
        _mockUserRepository.Verify(repo => repo.InsertOneAsync(It.Is<User>(u => u.Id == 123 && u.Username == "testuser")), Times.Once);
    }
    [Fact]
    public async Task UpdateHandler_ShouldNotProcessMessage_WhenUserIsGuest()
    {
        // Arrange
        var update = CreateMessageUpdate(123, "guestuser", "/generate");

        _mockUserRepository.Setup(repo => repo.FindByIdAsync(It.IsAny<long>()))
            .ReturnsAsync(new User { Id = 123, Username = "guestuser", Role = UserRole.Guest });

        // Act
        await _telegramBotService.UpdateHandler(_mockBotClient.Object, update, CancellationToken.None);

        // Assert

        _mockTelegramBotClientWrapper.Verify(
            bot => bot.SendMessage(
                It.IsAny<ChatId>(),
                It.IsAny<string>(),
                It.IsAny<int?>(),
                It.IsAny<ParseMode>(),
                It.IsAny<IEnumerable<MessageEntity>>(),
                It.IsAny<LinkPreviewOptions>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<string>(),
                It.IsAny<ReplyParameters>(),
                It.IsAny<IReplyMarkup>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()
            ),
            Times.Never
        );
    }
    [Fact]
    public async Task UpdateHandler_ShouldHandleAdminUsersCommand()
    {
        // Arrange
        var update = CreateMessageUpdate(123, "adminuser", "/admin users", 456);

        var adminUser  = new User { Id = 123, Username = "adminuser", Role = UserRole.Admin };
        _mockUserRepository.Setup(repo => repo.FindByIdAsync(It.IsAny<long>()))
            .ReturnsAsync(adminUser );

        var users = new List<User>
        {
            new User { Id = 1, Username = "user1", Role = UserRole.User },
            new User { Id = 2, Username = "user2", Role = UserRole.User }
        };
        _mockUserRepository.Setup(repo => repo.GetAllAsync())
            .ReturnsAsync(users);

        // Act
        await _telegramBotService.UpdateHandler(_mockBotClient.Object, update, CancellationToken.None);

        // Assert
        _mockTelegramBotClientWrapper.Verify(bot => bot.SendMessage(
            It.IsAny<ChatId>(),
            It.Is<string>(s => s.Contains("Список користувачів")),
            It.IsAny<int?>(),
            It.IsAny<ParseMode>(),
            It.IsAny<IEnumerable<MessageEntity>>(),
            It.IsAny<LinkPreviewOptions>(),
            It.IsAny<bool>(),
            It.IsAny<bool>(),
            It.IsAny<bool>(),
            It.IsAny<string>(),
            It.IsAny<ReplyParameters>(),
            It.IsAny<IReplyMarkup>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }
    [Fact]
    public async Task UpdateHandler_ShouldRespondToStartCommand()
    {
        // Arrange
        var update = CreateMessageUpdate(123, "testuser", "/start", 456);

        var user = new User { Id = 123, Username = "testuser", Role = UserRole.User };
        _mockUserRepository.Setup(repo => repo.FindByIdAsync(It.IsAny<long>()))
            .ReturnsAsync(user);

        var userScript = new PhpScript { Id = 123, State = PhpScriptState.None };
        _mockPhpScriptRepository.Setup(repo => repo.FindByIdAsync(It.IsAny<long>()))
            .ReturnsAsync(userScript);

        // Act
        await _telegramBotService.UpdateHandler(_mockBotClient.Object,update, CancellationToken.None);

        // Assert
        _mockTelegramBotClientWrapper.Verify(bot => bot.SendMessage(
            It.Is<ChatId>(id => id.Identifier == 456),
            It.Is<string>(s => s.Contains("Привіт! Уведіть /generate для створення скрипта.")),
            It.IsAny<int?>(),
            It.IsAny<ParseMode>(),
            It.IsAny<IEnumerable<MessageEntity>>(),
            It.IsAny<LinkPreviewOptions>(),
            It.IsAny<bool>(),
            It.IsAny<bool>(),
            It.IsAny<bool>(),
            It.IsAny<string>(),
            It.IsAny<ReplyParameters>(),
            It.IsAny<IReplyMarkup>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }
    [Fact]
    public async Task UpdateHandler_ShouldRespondToGenerateCommand_WhenUserHasPermission()
    {
        // Arrange
        var update = CreateMessageUpdate(123, "testuser", "/generate", 456);

        var user = new User { Id = 123, Username = "testuser", Role = UserRole.User };
        _mockUserRepository.Setup(repo => repo.FindByIdAsync(It.IsAny<long>()))
            .ReturnsAsync(user);

        var userScript = new PhpScript { Id = 123, State = PhpScriptState.None };
        _mockPhpScriptRepository.Setup(repo => repo.FindByIdAsync(It.IsAny<long>()))
            .ReturnsAsync(userScript);

        // Act
        await _telegramBotService.UpdateHandler(_mockBotClient.Object,update, CancellationToken.None);

        // Assert
        _mockTelegramBotClientWrapper.Verify(bot => bot.SendMessage(
            It.Is<ChatId>(id => id.Identifier == 456),
            It.Is<string>(s => s.Contains("Уведіть назву додатку (AppName):")),
            It.IsAny<int?>(),
            It.IsAny<ParseMode>(),
            It.IsAny<IEnumerable<MessageEntity>>(),
            It.IsAny<LinkPreviewOptions>(),
            It.IsAny<bool>(),
            It.IsAny<bool>(),
            It.IsAny<bool>(),
            It.IsAny<string>(),
            It.IsAny<ReplyParameters>(),
            It.IsAny<IReplyMarkup>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }
    private Update CreateMessageUpdate(long userId, string username, string text, long chatId = 0)
    {
        var user = new Telegram.Bot.Types.User { Id = userId, Username = username };
        var chat = new Chat { Id = chatId, Type = ChatType.Private };
        var message = new Message { From = user, Text = text, Chat = chat };
        var update = new Update
        {
            Message = message
        };

        return update;
    }
    
   
}