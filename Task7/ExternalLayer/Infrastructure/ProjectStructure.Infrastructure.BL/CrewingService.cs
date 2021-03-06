﻿using ProjectStructure.Domain;
using ProjectStructure.Domain.Interfaces;
using ProjectStructure.Services.Interfaces;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Net;
using System.Threading;
using Newtonsoft.Json;
using ProjectStructure.Infrastructure.Shared;
using AutoMapper;
using ProjectStructure.Infrastructure.Shared.Helpers;

namespace ProjectStructure.Infrastructure.BL
{
    public class CrewingService : ICrewingService
    {
        private readonly IDbCrewingUnitOfWork uow;
        private readonly IMapper mapper;
        private static SemaphoreSlim semaphore = new SemaphoreSlim(1);
        private readonly FileLogService logService;
        public CrewingService(IDbCrewingUnitOfWork crewingUnitOfWork, IMapper mapper)
        {
            this.mapper = mapper;
            uow = crewingUnitOfWork;
            logService = new FileLogService();
        }

        #region Crews

        public async Task LoadOutSourceCrewsAsync(string uri, int count = -1, CancellationToken ct = default(CancellationToken))
        {
            await semaphore.WaitAsync();
            try
            {
                if (string.IsNullOrEmpty(uri))
                    throw new ArgumentNullException("Uri is null!");

                var jsonData = string.Empty;
                using (var wClient = new WebClient())
                {

                    try
                    {
                       
                        jsonData = await wClient.DownloadStringTaskAsync(uri);
                        if (String.IsNullOrEmpty(jsonData))
                            throw new ArgumentNullException("Failed to load string data!");

                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                }

                IEnumerable<CrewExtendedDTO> crews = null;
                try
                {
                    crews = await Task.Run(() => {
                        return JsonConvert.DeserializeObject<IEnumerable<CrewExtendedDTO>>(jsonData) ?? null;
                    }, ct);
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                

                if (crews == null)
                    throw new ArgumentNullException("Failed to deserialize data!");

                if (count > 0)
                    crews = crews.Take(count);

                var c = mapper.Map<IEnumerable<Crew>>(crews);
                try
                {
                    
                    await uow.Crews.InsertRangeAsync(mapper.Map<IEnumerable<Crew>>(c), ct);
                    await uow.SaveChangesAsync(ct);
                    await logService.WriteLogAsync(await logService.FormCrewsText(c, ct), ct);
                    //await Task.WhenAll
                    //(
                    //,
                    //,
                    
                    //);
                }catch(Exception ex)
                {
                    throw ex;
                }
            }
            finally
            {
                semaphore.Release();
            }
        }
        

        public Crew GetCrewInfo(long id)
        {
            return uow.Crews.Get(id) ?? null;
        }

        public async Task<Crew> GetCrewInfoAsync(long id, CancellationToken ct)
        {
            return await uow.Crews.GetAsync(id, ct);
        }

        public Crew GetIncludedCrewInfo(long id, bool isCatched = false)
        {
            return uow.Crews.FindByInclude(e => e.Id == id, isCatched, 
                e => e.Pilot, e=> e.Stewardesses)
                .FirstOrDefault()
                ?? null;
        }

        public IEnumerable<Crew> GetAllCrewsInfo()
        {
            return uow.Crews.GetAll() ?? null;
        }

        public async Task<IEnumerable<Crew>> GetAllCrewsInfoAsync(CancellationToken ct = default(CancellationToken))
        {
            return await uow.Crews.GetAllAsync(ct) ?? null;
        }

        public Crew CreateCrew(long pilotId, IEnumerable<long> stewardessesIds)
        {
            var pilot = uow.Pilots.Get(pilotId);
            if (pilot == null)
                return null;
            var stewardesses = uow.Stewardesses.FindBy(s => stewardessesIds.Contains(s.Id));
            if (stewardesses == null || stewardesses.Count() < 2)
                return null;

            var item = uow.Crews.Update(new Crew(pilot, stewardesses.ToList()));
            if (item == null)
                return null;
            else
                uow.SaveChanges();
            return item;
        }

        public async Task<Crew> CreateCrewAsync(long pilotId, IEnumerable<long> stewardessesIds, CancellationToken ct = default(CancellationToken))
        {
            var pilot = await uow.Pilots.GetAsync(pilotId, ct);
            if (pilot == null)
                return null;
            var stewardesses = uow.Stewardesses.FindBy(s => stewardessesIds.Contains(s.Id));
            if (stewardesses == null || stewardesses.Count() < 2)
                return null;

            var item = uow.Crews.Update(new Crew(pilot, stewardesses.ToList()));
            if (item == null)
                return null;
            else
                await uow.SaveChangesAsync(ct);
            return item;
        }

        public Crew ReformCrew(long crewId, long pilotId, IEnumerable<long> stewardessesIds)
        {
            var crew = uow.Crews.Get(crewId);
            if (crew == null)
                return null;

            if(crew.Pilot.Id != pilotId)
            {
                var pilot = uow.Pilots.Get(pilotId);
                if (pilot == null)
                    return null;
                crew.Pilot = pilot;
            }
            
            var stewardesses = uow.Stewardesses.FindBy(s => stewardessesIds.Contains(s.Id));
            if (stewardesses == null || stewardesses.Count() < 2)
                return null;
            crew.Stewardesses = stewardesses.ToList();

            var item = uow.Crews.Update(crew);
            if (item == null)
                return null;
            else
            {
                uow.SaveChanges();
                return item;
            }
        }

        public async Task<Crew> ReformCrewAsync(long crewId, long pilotId, IEnumerable<long> stewardessesIds, CancellationToken ct = default(CancellationToken))
        {
            var crew = await uow.Crews.GetAsync(crewId, ct);
            if (crew == null)
                return null;

            if (crew.Pilot.Id != pilotId)
            {
                var pilot = await uow.Pilots.GetAsync(pilotId, ct);
                if (pilot == null)
                    return null;
                crew.Pilot = pilot;
            }

            var stewardesses = uow.Stewardesses.FindBy(s => stewardessesIds.Contains(s.Id));
            if (stewardesses == null || stewardesses.Count() < 2)
                return null;
            crew.Stewardesses = stewardesses.ToList();

            var item = uow.Crews.Update(crew);
            if (item == null)
                return null;
            else
            {
                await uow.SaveChangesAsync(ct);
                return item;
            }
        }

        public bool TryDeleteCrew(long id)
        {
            if (uow.Crews.Delete(id))
            {
                uow.SaveChanges();
                return true;
            }
            return false;
        }

        public async Task<bool> TryDeleteCrewAsync(long id, CancellationToken ct = default(CancellationToken))
        {
            if (uow.Crews.Delete(id))
            {
                await uow.SaveChangesAsync(ct);
                return true;
            }
            return false;
        }

        #endregion

        #region Pilots
        
        public Pilot GetPilotInfo(long id)
        {
            return uow.Pilots.Get(id) ?? null;
        }

        public async Task<Pilot> GetPilotInfoAsync(long id, CancellationToken ct = default(CancellationToken))
        {
            return await uow.Pilots.GetAsync(id, ct) ?? null;
        }

        public IEnumerable<Pilot> GetAllPilotsInfo()
        {
            return uow.Pilots.GetAll() ?? null;
        }

        public async Task<IEnumerable<Pilot>> GetAllPilotsInfoAsync(CancellationToken ct = default(CancellationToken))
        {
            return await uow.Pilots.GetAllAsync(ct);
        }

        public Pilot HirePilot(Pilot pilot)
        {
            var item = uow.Pilots.Insert(pilot);
            if (item == null)
                return null;
            else
                uow.SaveChanges();
            return item;
        }

        public async Task<Pilot> HirePilotAsync(Pilot pilot, CancellationToken ct = default(CancellationToken))
        {
            var item = await uow.Pilots.InsertAsync(pilot, ct);
            if (item == null)
                return null;
            else
                await uow.SaveChangesAsync(ct);
            return item;
        }

        public bool TryDismissPilot(long id)
        {
            if (uow.Pilots.Delete(id))
            {
                uow.SaveChanges();
                return true;
            }
            return false;
        }

        public async Task<bool> TryDismissPilotAsync(long id, CancellationToken ct = default(CancellationToken))
        {
            if (uow.Pilots.Delete(id))
            {
                await uow.SaveChangesAsync(ct);
                return true;
            }
            return false;
        }

        public Pilot UpdatePilotInfo(long id, Pilot pilot)
        {
            pilot.Id = id;
            var item = uow.Pilots.Update(pilot);
            if (item == null)
                return null;
            else
            {
                uow.SaveChanges();
                return item;
            }
        }

        public async Task<Pilot> UpdatePilotInfoAsync(long id, Pilot pilot, CancellationToken ct = default(CancellationToken))
        {
            pilot.Id = id;
            var item = uow.Pilots.Update(pilot);
            if (item == null)
                return null;
            else
            {
                await uow.SaveChangesAsync(ct);
                return item;
            }
        }

        #endregion

        #region Stewardesses

        public Stewardess GetStewardessInfo(long id)
        {
            return uow.Stewardesses.Get(id) ?? null;
        }
        
        public async Task<Stewardess> GetStewardessInfoAsync(long id, CancellationToken ct = default(CancellationToken))
        {
            return await uow.Stewardesses.GetAsync(id, ct) ?? null;
        }

        public IEnumerable<Stewardess> GetAllStewardessesInfo()
        {
            return uow.Stewardesses.GetAll() ?? null;
        }

        public async Task<IEnumerable<Stewardess>> GetAllStewardessesInfoAsync(CancellationToken ct = default(CancellationToken))
        {
            return await uow.Stewardesses.GetAllAsync(ct) ?? null;
        }

        public Stewardess HireStewardess(Stewardess stewardess)
        {
            var item = uow.Stewardesses.Insert(stewardess);
            if (item == null)
                return null;
            else
                uow.SaveChanges();
            return item;
        }

        public async Task<Stewardess> HireStewardessAsync(Stewardess stewardess, CancellationToken ct = default(CancellationToken))
        {
            var item = await uow.Stewardesses.InsertAsync(stewardess, ct);
            if (item == null)
                return null;
            else
                await uow.SaveChangesAsync(ct);
            return item;
        }

        public Stewardess UpdateStewardessInfo(long id, Stewardess stewardess)
        {
            stewardess.Id = id;
            var item = uow.Stewardesses.Update(stewardess);
            if (item == null)
                return null;
            else
            {
                uow.SaveChanges();
                return item;
            }
        }

        public async Task<Stewardess> UpdateStewardessInfoAsync(long id, Stewardess stewardess, CancellationToken ct = default(CancellationToken))
        {
            stewardess.Id = id;
            var item = uow.Stewardesses.Update(stewardess);
            if (item == null)
                return null;
            else
            {
                await uow.SaveChangesAsync(ct);
                return item;
            }
        }

        public bool TryDismissStewardess(long id)
        {
            if (uow.Stewardesses.Delete(id))
            {
                uow.SaveChanges();
                return true;
            }
            return false;
        }

        public async Task<bool> TryDismissStewardessAsync(long id, CancellationToken ct = default(CancellationToken))
        {
            if (uow.Stewardesses.Delete(id))
            {
                await uow.SaveChangesAsync(ct);
                return true;
            }
            return false;
        }

        #endregion
    }
}
