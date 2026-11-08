using System.ComponentModel.DataAnnotations;

namespace {{DBPROJECT}};

public class Dummy
{
  public int Id { get; set; }

  [MaxLength(100)]
  public string Name { get; set; } = "";

  public DateTime CreatedAt { get; set; }
}
