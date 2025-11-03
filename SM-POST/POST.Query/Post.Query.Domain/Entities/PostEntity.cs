using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Post.Query.Domain.Entities;

[Table("Post")]
public class PostEntity
{
    [Key]
    public Guid PostId { get; set; }
    public string Author { get; set; } = string.Empty;
    public DateTime DatePosted { get; set; }
    public string Message { get; set; } = string.Empty;
    public int Likes { get; set; }

    // navigate
    public virtual ICollection<CommentEntity> Comments { get; set; }

}
