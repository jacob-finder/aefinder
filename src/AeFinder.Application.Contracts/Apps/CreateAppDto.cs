using System.ComponentModel.DataAnnotations;

namespace AeFinder.Apps;

public class CreateAppDto
{
    public string AppId { get; set; }
    public string DeployKey { get; set; }
    [MinLength(2),MaxLength(20)]
    [RegularExpression("[A-Za-z0-9\\s]+")]
    public string AppName { get; set; }
    [MaxLength(200)]
    public string ImageUrl { get; set; }
    [MaxLength(200)]
    public string Description { get; set; }
    [MaxLength(200)]
    public string SourceCodeUrl { get; set; }
}