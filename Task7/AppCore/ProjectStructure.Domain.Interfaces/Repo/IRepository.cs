﻿using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace ProjectStructure.Domain.Interfaces
{
    public interface IRepository<T> where T : Entity
    {
        IEnumerable<T> GetAll();

        T Get(long id);

        T Insert(T entity);

        T Update(T entity);

        bool Delete(long id);
    }
}
