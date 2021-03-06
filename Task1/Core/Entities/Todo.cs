﻿using System;

namespace Core.Entities
{
    public class Todo : IEquatable<Todo>
    {
        public int Id { get; set; }

        public DateTime CreatedAt { get; set; }

        public string Name { get; set; }

        public bool IsComplete { get; set; }

        public int UserId { get; set; }

        public bool Equals(Todo other)
        {
            if (other == null)
                return false;
            if (ReferenceEquals(this, other))
                return true;

            return Id == other.Id && DateTime.Equals(CreatedAt, other.CreatedAt) && String.Equals(Name, other.Name)
                && UserId == other.UserId && IsComplete == other.IsComplete;
        }

        public override string ToString()
        {
            string state = this.IsComplete ? "completed" : "uncompleted";
            return "\t" + new string('-', 92) +
                $"\n\tid:{Id}| userId:{UserId}| \n\t{Name}  -  {state}\n\t createdAt{CreatedAt.ToString()}\n"
                + "\t" + new string('-', 92);
        }
    }
}
