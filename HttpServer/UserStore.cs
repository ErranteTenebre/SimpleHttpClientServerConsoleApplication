using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Entities;

namespace HttpServer
{
    public class UserStore
    {
        private List<User> _users = new List<User>();
        public List<User> Users
        {
            get
            {
                return _users;    
            }
        }
        public void Add(User user)
        {
            _users.Add(user);
        }
        public bool Edit(string userId, string name, int age)
        {
            User? userToEdit = _users.FirstOrDefault(us=> us.Id == userId);

            if (userToEdit == null) return false;

            userToEdit.Age = age;
            userToEdit.Name = name;

            return true;
        }

        public bool Remove(string id) {
            User? person = _users.FirstOrDefault((person)=> person.Id == id);

            if (person == null) return false;
            else
            {
                _users.Remove(person);
                return true;
            }
            
        }

        public void Clear()
        {
            _users.Clear(); 
        }
    }
}
