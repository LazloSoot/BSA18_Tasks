﻿using System.Threading.Tasks;

namespace ProjectStructure.Domain.Interfaces
{
    public interface IAirportDbContext
    {
        int SaveChanges();
        Task<int> SaveChangesAsync();
    }
}
