#nullable enable
using System.Collections.ObjectModel;
using System.Windows.Input;
using MovieApp.Core.Interfaces;
using MovieApp.Core.Models;
using MovieApp.Core.Services;

namespace MovieApp.UI.ViewModels;

/// <summary>
/// ViewModel for the movie detail view showing reviews, ratings, and external critic data.
/// </summary>
public class MovieDetailViewModel : ViewModelBase
{
    private readonly IReviewService _reviewService;
    private readonly ICommentService _commentService;
    private readonly ExternalReviewService _externalReviewService;
    private readonly int _currentUserId;

    private Movie? _movie;
    private double _averageRating;
    private float _newReviewRating;
    private string _newReviewContent = string.Empty;
    private string _newCommentContent = string.Empty;
    private bool _hasUserReview;
    private bool _showExtraReviewForm;
    private string _statusMessage = string.Empty;
    private bool _isLoadingExternalReviews;
    private double _criticScore;
    private double _audienceScore;
    private bool _isPolarized;
    private int _replyToCommentId;
    private string _replyContent = string.Empty;

    // Extra review fields
    private int _cinRating;
    private string _cinText = string.Empty;
    private int _actingRating;
    private string _actingText = string.Empty;
    private int _cgiRating;
    private string _cgiText = string.Empty;
    private int _plotRating;
    private string _plotText = string.Empty;
    private int _soundRating;
    private string _soundText = string.Empty;
    private string _mainExtraText = string.Empty;

    /// <summary>
    /// Event raised when navigating back to catalog.
    /// </summary>
    public event Action? NavigateBack;

    /// <summary>
    /// Initializes a new instance of <see cref="MovieDetailViewModel"/>.
    /// </summary>
    public MovieDetailViewModel(IReviewService reviewService, ICommentService commentService,
        ExternalReviewService externalReviewService, int currentUserId = 1)
    {
        _reviewService = reviewService;
        _commentService = commentService;
        _externalReviewService = externalReviewService;
        _currentUserId = currentUserId;

        SubmitReviewCommand = new AsyncRelayCommand(async _ => await SubmitReviewAsync());
        SubmitExtraReviewCommand = new AsyncRelayCommand(async _ => await SubmitExtraReviewAsync());
        ShowExtraReviewFormCommand = new RelayCommand(_ => ShowExtraReviewForm = true);
        AddCommentCommand = new AsyncRelayCommand(async _ => await AddCommentAsync());
        SubmitReplyCommand = new AsyncRelayCommand(async _ => await SubmitReplyAsync());
        StartReplyCommand = new RelayCommand(param =>
        {
            if (param is int commentId)
                ReplyToCommentId = commentId;
        });
        CancelReplyCommand = new RelayCommand(_ =>
        {
            ReplyContent = string.Empty;
            ReplyToCommentId = 0;
        });
        BackCommand = new RelayCommand(_ => NavigateBack?.Invoke());
        DeleteReviewCommand = new AsyncRelayCommand(async param =>
        {
            if (param is int reviewId)
                await DeleteReviewAsync(reviewId);
        });
    }

    /// <summary>Gets the collection of reviews for this movie.</summary>
    public ObservableCollection<Review> Reviews { get; } = new();

    /// <summary>Gets the collection of comments for this movie.</summary>
    public ObservableCollection<Comment> Comments { get; } = new();

    /// <summary>Gets the collection of root-level comments for this movie.</summary>
    public ObservableCollection<Comment> RootComments { get; } = new();

    /// <summary>Gets the collection of external critic reviews.</summary>
    public ObservableCollection<CriticReview> ExternalReviews { get; } = new();

    /// <summary>Gets the lexicon analysis results.</summary>
    public ObservableCollection<string> LexiconWords { get; } = new();

    /// <summary>Gets or sets the current movie.</summary>
    public Movie? Movie
    {
        get => _movie;
        set => SetProperty(ref _movie, value);
    }

    /// <summary>Gets or sets the average rating.</summary>
    public double AverageRating
    {
        get => _averageRating;
        set => SetProperty(ref _averageRating, value);
    }

    /// <summary>Gets or sets the new review star rating.</summary>
    public float NewReviewRating
    {
        get => _newReviewRating;
        set => SetProperty(ref _newReviewRating, value);
    }

    /// <summary>Gets or sets the new review content.</summary>
    public string NewReviewContent
    {
        get => _newReviewContent;
        set => SetProperty(ref _newReviewContent, value);
    }

    /// <summary>Gets or sets the new comment content.</summary>
    public string NewCommentContent
    {
        get => _newCommentContent;
        set => SetProperty(ref _newCommentContent, value);
    }

    /// <summary>Gets or sets whether the current user has reviewed this movie.</summary>
    public bool HasUserReview
    {
        get => _hasUserReview;
        set => SetProperty(ref _hasUserReview, value);
    }

    /// <summary>Gets or sets whether the extra review form is visible.</summary>
    public bool ShowExtraReviewForm
    {
        get => _showExtraReviewForm;
        set => SetProperty(ref _showExtraReviewForm, value);
    }

    /// <summary>Gets or sets a status message.</summary>
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    /// <summary>Gets or sets whether external reviews are loading.</summary>
    public bool IsLoadingExternalReviews
    {
        get => _isLoadingExternalReviews;
        set => SetProperty(ref _isLoadingExternalReviews, value);
    }

    /// <summary>Gets or sets the critic score.</summary>
    public double CriticScore
    {
        get => _criticScore;
        set => SetProperty(ref _criticScore, value);
    }

    /// <summary>Gets or sets the audience score.</summary>
    public double AudienceScore
    {
        get => _audienceScore;
        set => SetProperty(ref _audienceScore, value);
    }

    /// <summary>Gets or sets whether scores are polarized.</summary>
    public bool IsPolarized
    {
        get => _isPolarized;
        set => SetProperty(ref _isPolarized, value);
    }

    /// <summary>Gets or sets the comment ID being replied to.</summary>
    public int ReplyToCommentId
    {
        get => _replyToCommentId;
        set => SetProperty(ref _replyToCommentId, value);
    }

    /// <summary>Gets or sets the reply content.</summary>
    public string ReplyContent
    {
        get => _replyContent;
        set => SetProperty(ref _replyContent, value);
    }

    // Extra review properties
    /// <summary>Gets or sets the cinematography rating.</summary>
    public int CinRating { get => _cinRating; set => SetProperty(ref _cinRating, value); }
    /// <summary>Gets or sets the cinematography text.</summary>
    public string CinText { get => _cinText; set => SetProperty(ref _cinText, value); }
    /// <summary>Gets or sets the acting rating.</summary>
    public int ActingRating { get => _actingRating; set => SetProperty(ref _actingRating, value); }
    /// <summary>Gets or sets the acting text.</summary>
    public string ActingText { get => _actingText; set => SetProperty(ref _actingText, value); }
    /// <summary>Gets or sets the CGI rating.</summary>
    public int CgiRating { get => _cgiRating; set => SetProperty(ref _cgiRating, value); }
    /// <summary>Gets or sets the CGI text.</summary>
    public string CgiText { get => _cgiText; set => SetProperty(ref _cgiText, value); }
    /// <summary>Gets or sets the plot rating.</summary>
    public int PlotRating { get => _plotRating; set => SetProperty(ref _plotRating, value); }
    /// <summary>Gets or sets the plot text.</summary>
    public string PlotText { get => _plotText; set => SetProperty(ref _plotText, value); }
    /// <summary>Gets or sets the sound rating.</summary>
    public int SoundRating { get => _soundRating; set => SetProperty(ref _soundRating, value); }
    /// <summary>Gets or sets the sound text.</summary>
    public string SoundText { get => _soundText; set => SetProperty(ref _soundText, value); }
    /// <summary>Gets or sets the main extra review text.</summary>
    public string MainExtraText { get => _mainExtraText; set => SetProperty(ref _mainExtraText, value); }

    /// <summary>Gets the command to submit a review.</summary>
    public ICommand SubmitReviewCommand { get; }
    /// <summary>Gets the command to submit an extra review.</summary>
    public ICommand SubmitExtraReviewCommand { get; }
    /// <summary>Gets the command to show the extra review form.</summary>
    public ICommand ShowExtraReviewFormCommand { get; }
    /// <summary>Gets the command to add a comment.</summary>
    public ICommand AddCommentCommand { get; }
    /// <summary>Gets the command to submit a reply.</summary>
    public ICommand SubmitReplyCommand { get; }
    /// <summary>Gets the command to start replying to a comment.</summary>
    public ICommand StartReplyCommand { get; }
    /// <summary>Gets the command to cancel replying to a comment.</summary>
    public ICommand CancelReplyCommand { get; }
    /// <summary>Gets the command to navigate back.</summary>
    public ICommand BackCommand { get; }
    /// <summary>Gets the command to delete a review.</summary>
    public ICommand DeleteReviewCommand { get; }

    /// <summary>
    /// Loads movie details, reviews, comments, and external reviews.
    /// </summary>
    /// <param name="movie">The movie to display.</param>
    public async Task LoadMovieAsync(Movie movie)
    {
        Movie = movie;
        StatusMessage = string.Empty;
        ShowExtraReviewForm = false;

        // Load reviews
        var reviews = await _reviewService.GetReviewsForMovie(movie.MovieId);
        Reviews.Clear();
        foreach (var review in reviews)
            Reviews.Add(review);

        AverageRating = await _reviewService.GetAverageRating(movie.MovieId);
        HasUserReview = reviews.Any(r => r.User?.UserId == _currentUserId);

        // Load comments
        var comments = await _commentService.GetCommentsForMovie(movie.MovieId);
        RebuildCommentTree(comments);

        // Load external reviews asynchronously
        _ = LoadExternalReviewsAsync(movie.Title, movie.Year);
    }

    /// <summary>
    /// Loads external critic reviews and aggregate scores.
    /// </summary>
    private async Task LoadExternalReviewsAsync(string movieTitle, int releaseYear)
    {
        IsLoadingExternalReviews = true;
        try
        {
            var reviews = await _externalReviewService.GetExternalReviews(movieTitle, releaseYear);
            ExternalReviews.Clear();
            foreach (var review in reviews)
                ExternalReviews.Add(review);

            var (criticScore, audienceScore) = await _externalReviewService.GetAggregateScores(movieTitle);
            CriticScore = criticScore;
            AudienceScore = audienceScore;
            IsPolarized = _externalReviewService.IsPolarized(criticScore, audienceScore, threshold: 0.3);

            var lexicon = _externalReviewService.AnalyseLexicon(reviews);
            LexiconWords.Clear();
            foreach (var (word, count) in lexicon)
                LexiconWords.Add($"{word} ({count})");
        }
        catch
        {
            ExternalReviews.Clear();
            LexiconWords.Clear();
        }
        finally
        {
            IsLoadingExternalReviews = false;
        }
    }

    /// <summary>
    /// Submits a new review for the current movie.
    /// </summary>
    private async Task SubmitReviewAsync()
    {
        if (Movie == null) return;

        try
        {
            await _reviewService.AddReview(_currentUserId, Movie.MovieId, NewReviewRating, NewReviewContent);
            StatusMessage = "Review submitted successfully!";
            NewReviewContent = string.Empty;
            NewReviewRating = 0;
            await LoadMovieAsync(Movie);
        }
        catch (InvalidOperationException ex)
        {
            StatusMessage = ex.Message;
        }
    }

    /// <summary>
    /// Submits an extra review with category ratings and text.
    /// </summary>
    private async Task SubmitExtraReviewAsync()
    {
        if (Movie == null) return;

        var userReview = Reviews.FirstOrDefault(r => r.User?.UserId == _currentUserId);
        if (userReview == null)
        {
            StatusMessage = "You must submit a regular review first.";
            return;
        }

        try
        {
            await _reviewService.SubmitExtraReview(userReview.ReviewId,
                CgiRating, CgiText, ActingRating, ActingText,
                PlotRating, PlotText, SoundRating, SoundText,
                CinRating, CinText, MainExtraText);
            StatusMessage = "Extra review submitted successfully!";
            ShowExtraReviewForm = false;
            await LoadMovieAsync(Movie);
        }
        catch (InvalidOperationException ex)
        {
            StatusMessage = ex.Message;
        }
    }

    /// <summary>
    /// Adds a comment to the current movie.
    /// </summary>
    private async Task AddCommentAsync()
    {
        if (Movie == null || string.IsNullOrWhiteSpace(NewCommentContent)) return;

        try
        {
            await _commentService.AddComment(_currentUserId, Movie.MovieId, NewCommentContent);
            NewCommentContent = string.Empty;
            var comments = await _commentService.GetCommentsForMovie(Movie.MovieId);
            RebuildCommentTree(comments);
        }
        catch (InvalidOperationException ex)
        {
            StatusMessage = ex.Message;
        }
    }

    /// <summary>
    /// Submits a reply to a comment.
    /// </summary>
    private async Task SubmitReplyAsync()
    {
        if (Movie == null || ReplyToCommentId <= 0 || string.IsNullOrWhiteSpace(ReplyContent)) return;

        try
        {
            await _commentService.AddReply(_currentUserId, ReplyToCommentId, ReplyContent);
            ReplyContent = string.Empty;
            ReplyToCommentId = 0;
            var comments = await _commentService.GetCommentsForMovie(Movie.MovieId);
            RebuildCommentTree(comments);
        }
        catch (InvalidOperationException ex)
        {
            StatusMessage = ex.Message;
        }
    }

    private void RebuildCommentTree(IEnumerable<Comment> comments)
    {
        Comments.Clear();
        RootComments.Clear();

        var commentList = comments.Select(CloneComment).ToList();
        var commentsById = new Dictionary<int, Comment>();

        foreach (var comment in commentList)
        {
            comment.Replies.Clear();
            Comments.Add(comment);
            commentsById[comment.MessageId] = comment;
        }

        foreach (var comment in commentList)
        {
            if (comment.ParentComment is not null &&
                comment.ParentComment.MessageId is int parentId &&
                commentsById.TryGetValue(parentId, out var parentComment))
            {
                parentComment.Replies.Add(comment);
            }
            else
            {
                RootComments.Add(comment);
            }
        }
    }

    private static Comment CloneComment(Comment comment)
    {
        return new Comment
        {
            MessageId = comment.MessageId,
            Content = comment.Content,
            CreatedAt = comment.CreatedAt,
            Author = comment.Author,
            Movie = comment.Movie,
            ParentComment = comment.ParentComment,
            Replies = new List<Comment>()
        };
    }

    /// <summary>
    /// Deletes a review by ID and refreshes.
    /// </summary>
    private async Task DeleteReviewAsync(int reviewId)
    {
        if (Movie == null) return;

        try
        {
            await _reviewService.DeleteReview(reviewId);
            StatusMessage = "Review deleted.";
            await LoadMovieAsync(Movie);
        }
        catch (InvalidOperationException ex)
        {
            StatusMessage = ex.Message;
        }
    }
}
