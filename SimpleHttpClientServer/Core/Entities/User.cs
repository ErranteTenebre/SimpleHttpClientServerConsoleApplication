namespace Core.Entities
{
    public class User
    {
        public string Id { get; set; }
        public string Name { get; set; }   
        public int Age { get; set; } 

        public User(string id, string name, int age)
        {
            Id = id;
            Name = name;
            Age = age;
        }   
    }
}
