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
    private readonly ICommentService commentService;
    private readonly ICatalogService catalogService;
    private readonly int currentUserId;

    private string newCommentContent = string.Empty;
    private string statusMessage = string.Empty;
    private int selectedMovieId;
    private Movie? selectedMovie;
    private int replyToCommentId;
    private string replyContent = string.Empty;

    /// <summary>
    /// Initializes a new instance of <see cref="ForumViewModel"/>.
    /// </summary>
    public ForumViewModel(ICommentService commentService, ICatalogService catalogService, int currentUserId = 1)
    {
        this.commentService = commentService;
        this.catalogService = catalogService;
        this.currentUserId = currentUserId;

        this.LoadCommentsCommand = new AsyncRelayCommand(async _ => await LoadCommentsAsync());
        this.AddCommentCommand = new AsyncRelayCommand(async _ => await AddCommentAsync());
        this.ReplyCommand = new AsyncRelayCommand(async _ => await ReplyAsync());
        this.StartReplyCommand = new RelayCommand(param =>
        {
            if (param is int commentId)
                this.ReplyToCommentId = commentId;
        });
        this.CancelReplyCommand = new RelayCommand(_ =>
        {
            this.ReplyContent = string.Empty;
            this.ReplyToCommentId = 0;
        });
        this.LoadMoviesCommand = new AsyncRelayCommand(async _ => await LoadMoviesAsync());
    }

    /// <summary>Gets the collection of comments to display.</summary>
    public ObservableCollection<Comment> Comments { get; } = new ();

    /// <summary>Gets the collection of root-level comments.</summary>
    public ObservableCollection<Comment> RootComments { get; } = new ();

    /// <summary>Gets the available movies for the forum.</summary>
    public ObservableCollection<Movie> Movies { get; } = new ();

    /// <summary>Gets or sets the new comment content.</summary>
    public string NewCommentContent
    {
        get => newCommentContent;
        set => SetProperty(ref newCommentContent, value);
    }

    /// <summary>Gets or sets the status message.</summary>
    public string StatusMessage
    {
        get => statusMessage;
        set => SetProperty(ref statusMessage, value);
    }

    /// <summary>Gets or sets the selected movie (drives ComboBox selection).</summary>
    public Movie? SelectedMovie
    {
        get => selectedMovie;
        set
        {
            if (SetProperty(ref selectedMovie, value))
            {
                this.SelectedMovieId = value?.MovieId ?? 0;
            }
        }
    }

    /// <summary>Gets or sets the selected movie ID for viewing comments.</summary>
    public int SelectedMovieId
    {
        get => selectedMovieId;
        set
        {
            if (SetProperty(ref selectedMovieId, value))
            {
                _ = LoadCommentsAsync();
            }
        }
    }

    /// <summary>Gets or sets the comment ID being replied to.</summary>
    public int ReplyToCommentId
    {
        get => replyToCommentId;
        set => SetProperty(ref replyToCommentId, value);
    }

    /// <summary>Gets or sets the reply content.</summary>
    public string ReplyContent
    {
        get => replyContent;
        set => SetProperty(ref replyContent, value);
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
        var movies = await catalogService.GetAllMovies();
        Movies.Clear();
        foreach (var movie in movies)
            Movies.Add(movie);

        if (Movies.Count > 0 && SelectedMovieId == 0)
            this.SelectedMovie = Movies[0];
    }

    /// <summary>
    /// Loads comments for the selected movie and builds tree structure.
    /// </summary>
    public async Task LoadCommentsAsync()
    {
        if (this.SelectedMovieId <= 0) return;

        var comments = await this.commentService.GetCommentsForMovie(this.SelectedMovieId);
        this.RebuildCommentTree(comments);
    }

    /// <summary>
    /// Adds a new root comment.
    /// </summary>
    private async Task AddCommentAsync()
    {
        if (SelectedMovieId <= 0 || string.IsNullOrWhiteSpace(NewCommentContent))
        {
            this.StatusMessage = "Please select a movie and enter comment content.";
            return;
        }

        try
        {
            await this.commentService.AddComment(this.currentUserId, this.SelectedMovieId, this.NewCommentContent);
            this.NewCommentContent = string.Empty;
            this.StatusMessage = "Comment posted!";
            await this.LoadCommentsAsync();
        }
        catch (InvalidOperationException ex)
        {
            this.StatusMessage = ex.Message;
        }
    }

    /// <summary>
    /// Adds a reply to an existing comment.
    /// </summary>
    private async Task ReplyAsync()
    {
        if (this.ReplyToCommentId <= 0 || string.IsNullOrWhiteSpace(this.ReplyContent))
        {
            this.StatusMessage = "Please enter reply content.";
            return;
        }

        try
        {
            await this.commentService.AddReply(this.currentUserId, this.ReplyToCommentId, this.ReplyContent);
            this.ReplyContent = string.Empty;
            this.ReplyToCommentId = 0;
            this.StatusMessage = "Reply posted!";
            await LoadCommentsAsync();
        }
        catch (InvalidOperationException ex)
        {
            this.StatusMessage = ex.Message;
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
