#nullable enable
using System.Collections.ObjectModel;
using System.Windows.Input;
using MovieApp.Core.Interfaces;
using MovieApp.Core.Models;

namespace MovieApp.UI.ViewModels;

/// <summary>
/// ViewModel for the forum view showing threaded comments.
/// </summary>
public class ForumViewModel : ViewModelBase
{
    private readonly ICommentService _commentService;
    private readonly ICatalogService _catalogService;
    private readonly int _currentUserId;

    private string _newCommentContent = string.Empty;
    private string _statusMessage = string.Empty;
    private int _selectedMovieId;
    private Movie? _selectedMovie;
    private int _replyToCommentId;
    private string _replyContent = string.Empty;

    /// <summary>
    /// Initializes a new instance of <see cref="ForumViewModel"/>.
    /// </summary>
    public ForumViewModel(ICommentService commentService, ICatalogService catalogService, int currentUserId = 1)
    {
        _commentService = commentService;
        _catalogService = catalogService;
        _currentUserId = currentUserId;

        LoadCommentsCommand = new AsyncRelayCommand(async _ => await LoadCommentsAsync());
        AddCommentCommand = new AsyncRelayCommand(async _ => await AddCommentAsync());
        ReplyCommand = new AsyncRelayCommand(async _ => await ReplyAsync());
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
        LoadMoviesCommand = new AsyncRelayCommand(async _ => await LoadMoviesAsync());
    }

    /// <summary>Gets the collection of comments to display.</summary>
    public ObservableCollection<Comment> Comments { get; } = new();

    /// <summary>Gets the collection of root-level comments.</summary>
    public ObservableCollection<Comment> RootComments { get; } = new();

    /// <summary>Gets the available movies for the forum.</summary>
    public ObservableCollection<Movie> Movies { get; } = new();

    /// <summary>Gets or sets the new comment content.</summary>
    public string NewCommentContent
    {
        get => _newCommentContent;
        set => SetProperty(ref _newCommentContent, value);
    }

    /// <summary>Gets or sets the status message.</summary>
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    /// <summary>Gets or sets the selected movie (drives ComboBox selection).</summary>
    public Movie? SelectedMovie
    {
        get => _selectedMovie;
        set
        {
            if (SetProperty(ref _selectedMovie, value))
            {
                SelectedMovieId = value?.MovieId ?? 0;
            }
        }
    }

    /// <summary>Gets or sets the selected movie ID for viewing comments.</summary>
    public int SelectedMovieId
    {
        get => _selectedMovieId;
        set
        {
            if (SetProperty(ref _selectedMovieId, value))
            {
                _ = LoadCommentsAsync();
            }
        }
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

    /// <summary>Gets the command to load comments.</summary>
    public ICommand LoadCommentsCommand { get; }

    /// <summary>Gets the command to add a comment.</summary>
    public ICommand AddCommentCommand { get; }

    /// <summary>Gets the command to reply to a comment.</summary>
    public ICommand ReplyCommand { get; }

    /// <summary>Gets the command to start a reply.</summary>
    public ICommand StartReplyCommand { get; }

    /// <summary>Gets the command to cancel replying.</summary>
    public ICommand CancelReplyCommand { get; }

    /// <summary>Gets the command to load movies.</summary>
    public ICommand LoadMoviesCommand { get; }

    /// <summary>
    /// Loads the list of available movies.
    /// </summary>
    public async Task LoadMoviesAsync()
    {
        var movies = await _catalogService.GetAllMovies();
        Movies.Clear();
        foreach (var movie in movies)
            Movies.Add(movie);

        if (Movies.Count > 0 && SelectedMovieId == 0)
            SelectedMovie = Movies[0];
    }

    /// <summary>
    /// Loads comments for the selected movie and builds tree structure.
    /// </summary>
    public async Task LoadCommentsAsync()
    {
        if (SelectedMovieId <= 0) return;

        var comments = await _commentService.GetCommentsForMovie(SelectedMovieId);
        RebuildCommentTree(comments);
    }

    /// <summary>
    /// Adds a new root comment.
    /// </summary>
    private async Task AddCommentAsync()
    {
        if (SelectedMovieId <= 0 || string.IsNullOrWhiteSpace(NewCommentContent))
        {
            StatusMessage = "Please select a movie and enter comment content.";
            return;
        }

        try
        {
            await _commentService.AddComment(_currentUserId, SelectedMovieId, NewCommentContent);
            NewCommentContent = string.Empty;
            StatusMessage = "Comment posted!";
            await LoadCommentsAsync();
        }
        catch (InvalidOperationException ex)
        {
            StatusMessage = ex.Message;
        }
    }

    /// <summary>
    /// Adds a reply to an existing comment.
    /// </summary>
    private async Task ReplyAsync()
    {
        if (ReplyToCommentId <= 0 || string.IsNullOrWhiteSpace(ReplyContent))
        {
            StatusMessage = "Please enter reply content.";
            return;
        }

        try
        {
            await _commentService.AddReply(_currentUserId, ReplyToCommentId, ReplyContent);
            ReplyContent = string.Empty;
            ReplyToCommentId = 0;
            StatusMessage = "Reply posted!";
            await LoadCommentsAsync();
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
}
