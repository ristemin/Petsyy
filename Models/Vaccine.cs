using System.Collections.Generic;

public class Vaccine
{
    public int Id { get; set; }
    public string Name { get; set; }
    public List<Pet> Pets { get; set; } = new List<Pet>();

}