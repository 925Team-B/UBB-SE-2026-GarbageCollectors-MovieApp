using Moq;
using MovieApp.UI.ViewModels;
using MovieApp.Core.Models;
using MovieApp.Core.Interfaces;

namespace MovieApp.Tests.Unit.ViewModels
{
    public class ProfileViewModelTests
    {
        private readonly Mock<IPointService> mockPointService;
        private readonly Mock<IBadgeService> mockBadgeService;
        private const int CurrentUserId = 1;

        public ProfileViewModelTests()
        {
            mockPointService = new Mock<IPointService>();
            mockBadgeService = new Mock<IBadgeService>();

            mockPointService.Setup(s => s.GetUserStats(It.IsAny<int>()))
                .ReturnsAsync(new UserStats { TotalPoints = 0, WeeklyScore = 0 });

            mockBadgeService.Setup(s => s.CheckAndAwardBadges(It.IsAny<int>()))
                .Returns(System.Threading.Tasks.Task.CompletedTask);
            mockBadgeService.Setup(s => s.GetAllBadges())
                .ReturnsAsync(new List<Badge>());
            mockBadgeService.Setup(s => s.GetUserBadges(It.IsAny<int>()))
                .ReturnsAsync(new List<Badge>());
        }

        private ProfileViewModel CreateViewModel() =>
            new (mockPointService.Object, mockBadgeService.Object, CurrentUserId);

        [Fact]
        public async Task LoadProfileAsync_WhenCalled_CallsCheckAndAwardBadgesFirst()
        {
            List<string> callOrder = new List<string>();
            mockBadgeService.Setup(s => s.CheckAndAwardBadges(CurrentUserId))
                .Callback(() => callOrder.Add("check"))
                .Returns(System.Threading.Tasks.Task.CompletedTask);
            mockPointService.Setup(s => s.GetUserStats(CurrentUserId))
                .Callback(() => callOrder.Add("stats"))
                .ReturnsAsync(new UserStats { TotalPoints = 0, WeeklyScore = 0 });

            ProfileViewModel vm = CreateViewModel();
            await vm.LoadProfileAsync();

            Assert.Equal("check", callOrder[0]);
            Assert.Equal("stats", callOrder[1]);
        }

        [Fact]
        public async Task LoadProfileAsync_WhenCalled_CallsGetUserStats()
        {
            ProfileViewModel vm = CreateViewModel();

            await vm.LoadProfileAsync();

            mockPointService.Verify(s => s.GetUserStats(CurrentUserId), Times.Once);
        }

        [Fact]
        public async Task LoadProfileAsync_WhenCalled_SetsTotalPoints()
        {
            mockPointService.Setup(s => s.GetUserStats(CurrentUserId))
                .ReturnsAsync(new UserStats { TotalPoints = 500, WeeklyScore = 0 });

            ProfileViewModel vm = CreateViewModel();
            await vm.LoadProfileAsync();

            Assert.Equal(500, vm.TotalPoints);
        }

        [Fact]
        public async Task LoadProfileAsync_WhenCalled_SetsWeeklyScore()
        {
            mockPointService.Setup(s => s.GetUserStats(CurrentUserId))
                .ReturnsAsync(new UserStats { TotalPoints = 0, WeeklyScore = 75 });

            ProfileViewModel vm = CreateViewModel();
            await vm.LoadProfileAsync();

            Assert.Equal(75, vm.WeeklyScore);
        }

        [Fact]
        public async Task LoadProfileAsync_WhenCalled_CallsGetAllBadgesAndGetUserBadges()
        {
            ProfileViewModel vm = CreateViewModel();

            await vm.LoadProfileAsync();

            mockBadgeService.Verify(s => s.GetAllBadges(), Times.Once);
            mockBadgeService.Verify(s => s.GetUserBadges(CurrentUserId), Times.Once);
        }

        [Fact]
        public async Task LoadProfileAsync_WhenBadgesLoaded_PopulatesAllBadgesCollection()
        {
            mockBadgeService.Setup(s => s.GetAllBadges()).ReturnsAsync(new List<Badge>
            {
                new () { BadgeId = 1, Name = "Reviewer" },
                new () { BadgeId = 2, Name = "Bettor" },
                new () { BadgeId = 3, Name = "Critic" }
            });
            mockBadgeService.Setup(s => s.GetUserBadges(CurrentUserId))
                .ReturnsAsync(new List<Badge>());

            ProfileViewModel vm = CreateViewModel();
            await vm.LoadProfileAsync();

            Assert.Equal(3, vm.AllBadges.Count);
        }

        [Fact]
        public async Task LoadProfileAsync_WhenUserHasEarnedBadge_MarksItAsUnlocked()
        {
            mockBadgeService.Setup(s => s.GetAllBadges()).ReturnsAsync(new List<Badge>
            {
                new () { BadgeId = 1, Name = "Reviewer" },
                new () { BadgeId = 2, Name = "Bettor" }
            });
            mockBadgeService.Setup(s => s.GetUserBadges(CurrentUserId))
                .ReturnsAsync(new List<Badge> { new () { BadgeId = 1, Name = "Reviewer" } });

            ProfileViewModel vm = CreateViewModel();
            await vm.LoadProfileAsync();

            BadgeDisplayItem reviewer = vm.AllBadges.First(b => b.Badge.BadgeId == 1);
            BadgeDisplayItem bettor = vm.AllBadges.First(b => b.Badge.BadgeId == 2);
            Assert.True(reviewer.IsUnlocked);
            Assert.False(bettor.IsUnlocked);
        }

        [Fact]
        public async Task LoadProfileAsync_WhenCalledTwice_ReplacesAllBadgesNotAppends()
        {
            mockBadgeService.Setup(s => s.GetAllBadges()).ReturnsAsync(new List<Badge>
            {
                new () { BadgeId = 1, Name = "Reviewer" }
            });

            ProfileViewModel vm = CreateViewModel();
            await vm.LoadProfileAsync();
            await vm.LoadProfileAsync();

            Assert.Single(vm.AllBadges);
        }

        [Fact]
        public async Task LoadProfileCommand_WhenExecuted_LoadsPointsAndBadges()
        {
            mockPointService.Setup(s => s.GetUserStats(CurrentUserId))
                .ReturnsAsync(new UserStats { TotalPoints = 100, WeeklyScore = 10 });

            ProfileViewModel vm = CreateViewModel();
            vm.LoadProfileCommand.Execute(null);
            await Task.Delay(150);

            Assert.Equal(100, vm.TotalPoints);
            Assert.Equal(10, vm.WeeklyScore);
        }

        [Fact]
        public async Task TotalPoints_WhenChanged_RaisesPropertyChangedEvent()
        {
            mockPointService.Setup(s => s.GetUserStats(CurrentUserId))
                .ReturnsAsync(new UserStats { TotalPoints = 99, WeeklyScore = 0 });

            ProfileViewModel vm = CreateViewModel();
            bool raised = false;
            vm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(vm.TotalPoints))
                {
                    raised = true;
                }
            };

            await vm.LoadProfileAsync();

            Assert.True(raised);
        }

        [Fact]
        public async Task WeeklyScore_WhenChanged_RaisesPropertyChangedEvent()
        {
            mockPointService.Setup(s => s.GetUserStats(CurrentUserId))
                .ReturnsAsync(new UserStats { TotalPoints = 0, WeeklyScore = 55 });

            ProfileViewModel vm = CreateViewModel();
            bool raised = false;
            vm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(vm.WeeklyScore))
{
    raised = true;
}
            };

            await vm.LoadProfileAsync();

            Assert.True(raised);
        }

        [Fact]
        public void TotalPoints_WhenSetDirectly_UpdatesProperty()
        {
            ProfileViewModel vm = CreateViewModel();

            vm.TotalPoints = 250;

            Assert.Equal(250, vm.TotalPoints);
        }

        [Fact]
        public void WeeklyScore_WhenSetDirectly_UpdatesProperty()
        {
            ProfileViewModel vm = CreateViewModel();

            vm.WeeklyScore = 40;

            Assert.Equal(40, vm.WeeklyScore);
        }
    }
}