using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Entities;

namespace HttpServer
{
    public class PersonStore
    {
        private List<Person> _persons = new List<Person>();
        public List<Person> Persons
        {
            get
            {
                return _persons;    
            }
        }
        public void Add(Person person)
        {
            _persons.Add(person);
        }

        public void Remove(string id) {
            Person? person = _persons.FirstOrDefault((person)=> person.id == id);

            if (person != null) {
                _persons.Remove(person);
            }
        }
    }
}
