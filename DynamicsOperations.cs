using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Dynamics365UserManager
{
    public class UserInfo
    {
        public Guid Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public Guid BusinessUnitId { get; set; }
        public string BusinessUnitName { get; set; }
    }

    public class RoleInfo
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public Guid BusinessUnitId { get; set; }
    }

    public class TeamInfo
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public bool IsDefault { get; set; }
        public string BusinessUnitName { get; set; }
    }

    public class BusinessUnitInfo
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
    }

    public class OperationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public List<string> Details { get; set; } = new List<string>();
    }

    public class RecordCounts
    {
        public int AccountCount { get; set; }
        public int ContactCount { get; set; }
        public int OpportunityCount { get; set; }
        public int QuoteCount { get; set; }
        public int OrderCount { get; set; }
        public int LeadCount { get; set; }
        public int CaseCount { get; set; }
    }

    public class PrivilegeRequirement
    {
        public string EntityLogicalName { get; set; }
        public string EntityDisplayName { get; set; }
        public string AccessRight { get; set; }
        public int MinDepthMask { get; set; }
        public string DepthDisplayName { get; set; }
        public override string ToString() => $"{AccessRight} {EntityDisplayName} ({DepthDisplayName})";
    }

    public class RoleCombinationResult
    {
        public List<string> RoleNames { get; set; } = new List<string>();
        public int Count => RoleNames.Count;
        public override string ToString() => string.Join(" + ", RoleNames);
    }

    public static class DynamicsOperations
    {
        public static List<UserInfo> SearchUsers(IOrganizationService service, string searchText)
        {
            var query = new QueryExpression("systemuser")
            {
                ColumnSet = new ColumnSet("fullname", "internalemailaddress", "businessunitid"),
                Criteria = new FilterExpression(LogicalOperator.And)
                {
                    Conditions =
                    {
                        new ConditionExpression("isdisabled", ConditionOperator.Equal, false)
                    },
                    Filters =
                    {
                        new FilterExpression(LogicalOperator.Or)
                        {
                            Conditions =
                            {
                                new ConditionExpression("fullname", ConditionOperator.Like, $"%{searchText}%"),
                                new ConditionExpression("internalemailaddress", ConditionOperator.Like, $"%{searchText}%")
                            }
                        }
                    }
                },
                TopCount = 50,
                Orders = { new OrderExpression("fullname", OrderType.Ascending) }
            };

            var results = service.RetrieveMultiple(query);
            var users = new List<UserInfo>();

            foreach (var entity in results.Entities)
            {
                var buRef = entity.GetAttributeValue<EntityReference>("businessunitid");
                users.Add(new UserInfo
                {
                    Id = entity.Id,
                    FullName = entity.GetAttributeValue<string>("fullname") ?? "",
                    Email = entity.GetAttributeValue<string>("internalemailaddress") ?? "",
                    BusinessUnitId = buRef?.Id ?? Guid.Empty,
                    BusinessUnitName = buRef?.Name ?? ""
                });
            }

            return users;
        }

        public static UserInfo SearchUserByEmail(IOrganizationService service, string email)
        {
            var query = new QueryExpression("systemuser")
            {
                ColumnSet = new ColumnSet("fullname", "internalemailaddress", "businessunitid"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("internalemailaddress", ConditionOperator.Equal, email),
                        new ConditionExpression("isdisabled", ConditionOperator.Equal, false)
                    }
                }
            };

            var results = service.RetrieveMultiple(query);
            if (results.Entities.Count == 0)
                return null;

            var entity = results.Entities[0];
            var buRef = entity.GetAttributeValue<EntityReference>("businessunitid");
            return new UserInfo
            {
                Id = entity.Id,
                FullName = entity.GetAttributeValue<string>("fullname") ?? "",
                Email = entity.GetAttributeValue<string>("internalemailaddress") ?? "",
                BusinessUnitId = buRef?.Id ?? Guid.Empty,
                BusinessUnitName = buRef?.Name ?? ""
            };
        }

        public static List<RoleInfo> GetUserRoles(IOrganizationService service, Guid userId)
        {
            var query = new QueryExpression("role")
            {
                ColumnSet = new ColumnSet("name", "businessunitid"),
                LinkEntities =
                {
                    new LinkEntity("role", "systemuserroles", "roleid", "roleid", JoinOperator.Inner)
                    {
                        LinkCriteria = new FilterExpression
                        {
                            Conditions =
                            {
                                new ConditionExpression("systemuserid", ConditionOperator.Equal, userId)
                            }
                        }
                    }
                }
            };

            var results = service.RetrieveMultiple(query);
            return results.Entities.Select(e => new RoleInfo
            {
                Id = e.Id,
                Name = e.GetAttributeValue<string>("name") ?? "",
                BusinessUnitId = e.GetAttributeValue<EntityReference>("businessunitid")?.Id ?? Guid.Empty
            }).ToList();
        }

        public static List<TeamInfo> GetUserTeams(IOrganizationService service, Guid userId)
        {
            var query = new QueryExpression("team")
            {
                ColumnSet = new ColumnSet("name", "isdefault"),
                LinkEntities =
                {
                    new LinkEntity("team", "teammembership", "teamid", "teamid", JoinOperator.Inner)
                    {
                        LinkCriteria = new FilterExpression
                        {
                            Conditions =
                            {
                                new ConditionExpression("systemuserid", ConditionOperator.Equal, userId)
                            }
                        }
                    }
                },
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("teamtype", ConditionOperator.Equal, 0)
                    }
                }
            };

            var results = service.RetrieveMultiple(query);
            return results.Entities.Select(e => new TeamInfo
            {
                Id = e.Id,
                Name = e.GetAttributeValue<string>("name") ?? "",
                IsDefault = e.GetAttributeValue<bool>("isdefault")
            }).ToList();
        }

        public static List<BusinessUnitInfo> GetAllBusinessUnits(IOrganizationService service)
        {
            var query = new QueryExpression("businessunit")
            {
                ColumnSet = new ColumnSet("name"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("isdisabled", ConditionOperator.Equal, false)
                    }
                },
                Orders = { new OrderExpression("name", OrderType.Ascending) }
            };

            var results = service.RetrieveMultiple(query);
            return results.Entities.Select(e => new BusinessUnitInfo
            {
                Id = e.Id,
                Name = e.GetAttributeValue<string>("name") ?? ""
            }).ToList();
        }

        public static OperationResult ChangeBusinessUnit(IOrganizationService service, Guid userId, Guid newBusinessUnitId, Action<string> log)
        {
            var result = new OperationResult();

            try
            {
                log("Recupero ruoli correnti...");
                var currentRoles = GetUserRoles(service, userId);
                var roleNames = currentRoles.Select(r => r.Name).Distinct().ToList();
                log($"Trovati {roleNames.Count} ruoli: {string.Join(", ", roleNames)}");

                log("Esecuzione cambio Business Unit...");
                var request = new SetBusinessSystemUserRequest
                {
                    UserId = userId,
                    BusinessId = newBusinessUnitId,
                    ReassignPrincipal = new EntityReference("systemuser", userId)
                };
                service.Execute(request);
                log("Business Unit cambiata con successo.");

                log("Ricerca ruoli equivalenti nella nuova BU...");
                var newRoles = GetRolesByNamesInBU(service, roleNames, newBusinessUnitId);
                log($"Trovati {newRoles.Count} ruoli equivalenti su {roleNames.Count}.");

                int assigned = 0;
                foreach (var role in newRoles)
                {
                    try
                    {
                        service.Associate("systemuser", userId,
                            new Relationship("systemuserroles_association"),
                            new EntityReferenceCollection { new EntityReference("role", role.Id) });
                        log($"  Ruolo assegnato: {role.Name}");
                        assigned++;
                        result.Details.Add($"Ruolo assegnato: {role.Name}");
                    }
                    catch (Exception ex)
                    {
                        log($"  Errore assegnazione ruolo {role.Name}: {ex.Message}");
                        result.Details.Add($"Errore ruolo {role.Name}: {ex.Message}");
                    }
                }

                var missing = roleNames.Except(newRoles.Select(r => r.Name)).ToList();
                if (missing.Any())
                {
                    log($"Ruoli non trovati nella nuova BU: {string.Join(", ", missing)}");
                    result.Details.Add($"Ruoli non trovati: {string.Join(", ", missing)}");
                }

                result.Success = true;
                result.Message = $"BU cambiata. {assigned}/{roleNames.Count} ruoli riassegnati.";
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Errore: {ex.Message}";
            }

            return result;
        }

        private static List<RoleInfo> GetRolesByNamesInBU(IOrganizationService service, List<string> roleNames, Guid businessUnitId)
        {
            if (!roleNames.Any()) return new List<RoleInfo>();

            var query = new QueryExpression("role")
            {
                ColumnSet = new ColumnSet("name", "businessunitid"),
                Criteria = new FilterExpression(LogicalOperator.And)
                {
                    Conditions =
                    {
                        new ConditionExpression("businessunitid", ConditionOperator.Equal, businessUnitId)
                    }
                }
            };

            var nameFilter = new FilterExpression(LogicalOperator.Or);
            foreach (var name in roleNames)
                nameFilter.Conditions.Add(new ConditionExpression("name", ConditionOperator.Equal, name));
            query.Criteria.Filters.Add(nameFilter);

            var results = service.RetrieveMultiple(query);
            return results.Entities.Select(e => new RoleInfo
            {
                Id = e.Id,
                Name = e.GetAttributeValue<string>("name") ?? "",
                BusinessUnitId = e.GetAttributeValue<EntityReference>("businessunitid")?.Id ?? Guid.Empty
            }).ToList();
        }

        public static OperationResult CloneUser(IOrganizationService service, UserInfo source, UserInfo target,
            bool copyBU, bool copyRoles, bool copyTeams, List<TeamInfo> selectedTeams, Action<string> log)
        {
            var result = new OperationResult();

            try
            {
                if (copyBU && source.BusinessUnitId != target.BusinessUnitId)
                {
                    log("Cambio BU del target...");
                    var buResult = ChangeBusinessUnit(service, target.Id, source.BusinessUnitId, log);
                    result.Details.AddRange(buResult.Details);
                    if (!buResult.Success)
                    {
                        result.Success = false;
                        result.Message = $"Errore cambio BU: {buResult.Message}";
                        return result;
                    }
                }

                if (copyRoles)
                {
                    log("Copia ruoli...");
                    var sourceRoles = GetUserRoles(service, source.Id);
                    var targetRoles = GetUserRoles(service, target.Id);
                    var targetRoleNames = new HashSet<string>(targetRoles.Select(r => r.Name));

                    var targetBuId = copyBU ? source.BusinessUnitId : target.BusinessUnitId;
                    var rolesToAssign = GetRolesByNamesInBU(service,
                        sourceRoles.Select(r => r.Name).Where(n => !targetRoleNames.Contains(n)).ToList(),
                        targetBuId);

                    foreach (var role in rolesToAssign)
                    {
                        try
                        {
                            service.Associate("systemuser", target.Id,
                                new Relationship("systemuserroles_association"),
                                new EntityReferenceCollection { new EntityReference("role", role.Id) });
                            log($"  Ruolo copiato: {role.Name}");
                            result.Details.Add($"Ruolo copiato: {role.Name}");
                        }
                        catch (Exception ex)
                        {
                            log($"  Errore ruolo {role.Name}: {ex.Message}");
                        }
                    }
                }

                if (copyTeams && selectedTeams != null)
                {
                    log("Aggiunta ai team...");
                    foreach (var team in selectedTeams.Where(t => !t.IsDefault))
                    {
                        try
                        {
                            var addRequest = new AddMembersTeamRequest
                            {
                                TeamId = team.Id,
                                MemberIds = new[] { target.Id }
                            };
                            service.Execute(addRequest);
                            log($"  Aggiunto al team: {team.Name}");
                            result.Details.Add($"Aggiunto al team: {team.Name}");
                        }
                        catch (Exception ex)
                        {
                            log($"  Errore team {team.Name}: {ex.Message}");
                        }
                    }
                }

                result.Success = true;
                result.Message = "Clonazione completata.";
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Errore: {ex.Message}";
            }

            return result;
        }

        public static RecordCounts CountRecords(IOrganizationService service, Guid ownerId)
        {
            var counts = new RecordCounts();
            counts.AccountCount = CountEntity(service, "account", ownerId);
            counts.ContactCount = CountEntity(service, "contact", ownerId);
            counts.OpportunityCount = CountEntity(service, "opportunity", ownerId);
            counts.QuoteCount = CountEntity(service, "quote", ownerId);
            counts.OrderCount = CountEntity(service, "salesorder", ownerId);
            counts.LeadCount = CountEntity(service, "lead", ownerId);
            counts.CaseCount = CountEntity(service, "incident", ownerId);
            return counts;
        }

        private static int CountEntity(IOrganizationService service, string entityName, Guid ownerId)
        {
            try
            {
                var query = new QueryExpression(entityName)
                {
                    ColumnSet = new ColumnSet(false),
                    Criteria = new FilterExpression
                    {
                        Conditions =
                        {
                            new ConditionExpression("ownerid", ConditionOperator.Equal, ownerId)
                        }
                    },
                    PageInfo = new PagingInfo { Count = 5000, PageNumber = 1 }
                };

                int total = 0;
                EntityCollection results;
                do
                {
                    results = service.RetrieveMultiple(query);
                    total += results.Entities.Count;
                    query.PageInfo.PageNumber++;
                    query.PageInfo.PagingCookie = results.PagingCookie;
                } while (results.MoreRecords);

                return total;
            }
            catch
            {
                return -1;
            }
        }

        public static OperationResult ReassignRecords(IOrganizationService service, Guid oldOwnerId, Guid newOwnerId,
            bool accounts, bool contacts, bool opportunities, bool quotes, bool orders, bool leads, bool cases,
            Action<string> log)
        {
            var result = new OperationResult();
            int totalReassigned = 0;

            try
            {
                var entities = new List<Tuple<string, bool>>
                {
                    Tuple.Create("account", accounts),
                    Tuple.Create("contact", contacts),
                    Tuple.Create("opportunity", opportunities),
                    Tuple.Create("quote", quotes),
                    Tuple.Create("salesorder", orders),
                    Tuple.Create("lead", leads),
                    Tuple.Create("incident", cases)
                };

                foreach (var ent in entities.Where(e => e.Item2))
                {
                    var entityName = ent.Item1;
                    log($"Riassegnazione {entityName}...");

                    var query = new QueryExpression(entityName)
                    {
                        ColumnSet = new ColumnSet(false),
                        Criteria = new FilterExpression
                        {
                            Conditions =
                            {
                                new ConditionExpression("ownerid", ConditionOperator.Equal, oldOwnerId)
                            }
                        },
                        PageInfo = new PagingInfo { Count = 5000, PageNumber = 1 }
                    };

                    int count = 0;
                    EntityCollection results;
                    do
                    {
                        results = service.RetrieveMultiple(query);
                        foreach (var record in results.Entities)
                        {
                            try
                            {
                                var assignRequest = new AssignRequest
                                {
                                    Assignee = new EntityReference("systemuser", newOwnerId),
                                    Target = new EntityReference(entityName, record.Id)
                                };
                                service.Execute(assignRequest);
                                count++;
                            }
                            catch (Exception ex)
                            {
                                log($"  Errore {entityName} {record.Id}: {ex.Message}");
                            }
                        }
                        query.PageInfo.PageNumber++;
                        query.PageInfo.PagingCookie = results.PagingCookie;
                    } while (results.MoreRecords);

                    log($"  {entityName}: {count} record riassegnati.");
                    result.Details.Add($"{entityName}: {count}");
                    totalReassigned += count;
                }

                result.Success = true;
                result.Message = $"Riassegnazione completata. {totalReassigned} record totali.";
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Errore: {ex.Message}";
            }

            return result;
        }

        // ─────────── Security Roles ───────────

        public static List<RoleInfo> SearchRoles(IOrganizationService service, string searchText)
        {
            var query = new QueryExpression("role")
            {
                ColumnSet = new ColumnSet("name", "businessunitid"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("name", ConditionOperator.Like, $"%{searchText}%")
                    }
                },
                TopCount = 100,
                Orders = { new OrderExpression("name", OrderType.Ascending) }
            };

            var results = service.RetrieveMultiple(query);
            return results.Entities.Select(e => new RoleInfo
            {
                Id = e.Id,
                Name = e.GetAttributeValue<string>("name") ?? "",
                BusinessUnitId = e.GetAttributeValue<EntityReference>("businessunitid")?.Id ?? Guid.Empty
            }).ToList();
        }

        public static List<UserInfo> GetUsersWithRole(IOrganizationService service, Guid roleId)
        {
            var query = new QueryExpression("systemuser")
            {
                ColumnSet = new ColumnSet("fullname", "internalemailaddress", "businessunitid"),
                LinkEntities =
                {
                    new LinkEntity("systemuser", "systemuserroles", "systemuserid", "systemuserid", JoinOperator.Inner)
                    {
                        LinkCriteria = new FilterExpression
                        {
                            Conditions =
                            {
                                new ConditionExpression("roleid", ConditionOperator.Equal, roleId)
                            }
                        }
                    }
                },
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("isdisabled", ConditionOperator.Equal, false)
                    }
                },
                Orders = { new OrderExpression("fullname", OrderType.Ascending) }
            };

            var results = service.RetrieveMultiple(query);
            return results.Entities.Select(e =>
            {
                var buRef = e.GetAttributeValue<EntityReference>("businessunitid");
                return new UserInfo
                {
                    Id = e.Id,
                    FullName = e.GetAttributeValue<string>("fullname") ?? "",
                    Email = e.GetAttributeValue<string>("internalemailaddress") ?? "",
                    BusinessUnitId = buRef?.Id ?? Guid.Empty,
                    BusinessUnitName = buRef?.Name ?? ""
                };
            }).ToList();
        }

        public static OperationResult AssignRoleToUsers(IOrganizationService service, Guid roleId, string roleName, List<Guid> userIds, Action<string> log)
        {
            var result = new OperationResult();
            int assigned = 0;

            try
            {
                foreach (var userId in userIds)
                {
                    try
                    {
                        service.Associate("systemuser", userId,
                            new Relationship("systemuserroles_association"),
                            new EntityReferenceCollection { new EntityReference("role", roleId) });
                        assigned++;
                    }
                    catch (Exception ex)
                    {
                        log($"  Errore assegnazione a utente {userId}: {ex.Message}");
                    }
                }

                result.Success = true;
                result.Message = $"Ruolo '{roleName}' assegnato a {assigned}/{userIds.Count} utenti.";
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Errore: {ex.Message}";
            }

            return result;
        }

        public static OperationResult RemoveRoleFromUsers(IOrganizationService service, Guid roleId, string roleName, List<Guid> userIds, Action<string> log)
        {
            var result = new OperationResult();
            int removed = 0;

            try
            {
                foreach (var userId in userIds)
                {
                    try
                    {
                        service.Disassociate("systemuser", userId,
                            new Relationship("systemuserroles_association"),
                            new EntityReferenceCollection { new EntityReference("role", roleId) });
                        removed++;
                    }
                    catch (Exception ex)
                    {
                        log($"  Errore rimozione da utente {userId}: {ex.Message}");
                    }
                }

                result.Success = true;
                result.Message = $"Ruolo '{roleName}' rimosso da {removed}/{userIds.Count} utenti.";
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Errore: {ex.Message}";
            }

            return result;
        }

        // ─────────── Teams ───────────

        public static List<TeamInfo> SearchTeams(IOrganizationService service, string searchText)
        {
            var query = new QueryExpression("team")
            {
                ColumnSet = new ColumnSet("name", "isdefault", "businessunitid"),
                Criteria = new FilterExpression(LogicalOperator.And)
                {
                    Conditions =
                    {
                        new ConditionExpression("name", ConditionOperator.Like, $"%{searchText}%"),
                        new ConditionExpression("teamtype", ConditionOperator.Equal, 0)
                    }
                },
                TopCount = 100,
                Orders = { new OrderExpression("name", OrderType.Ascending) }
            };

            var results = service.RetrieveMultiple(query);
            return results.Entities.Select(e =>
            {
                var buRef = e.GetAttributeValue<EntityReference>("businessunitid");
                return new TeamInfo
                {
                    Id = e.Id,
                    Name = e.GetAttributeValue<string>("name") ?? "",
                    IsDefault = e.GetAttributeValue<bool>("isdefault"),
                    BusinessUnitName = buRef?.Name ?? ""
                };
            }).ToList();
        }

        public static List<UserInfo> GetTeamMembers(IOrganizationService service, Guid teamId)
        {
            var query = new QueryExpression("systemuser")
            {
                ColumnSet = new ColumnSet("fullname", "internalemailaddress", "businessunitid"),
                LinkEntities =
                {
                    new LinkEntity("systemuser", "teammembership", "systemuserid", "systemuserid", JoinOperator.Inner)
                    {
                        LinkCriteria = new FilterExpression
                        {
                            Conditions =
                            {
                                new ConditionExpression("teamid", ConditionOperator.Equal, teamId)
                            }
                        }
                    }
                },
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("isdisabled", ConditionOperator.Equal, false)
                    }
                },
                Orders = { new OrderExpression("fullname", OrderType.Ascending) }
            };

            var results = service.RetrieveMultiple(query);
            return results.Entities.Select(e =>
            {
                var buRef = e.GetAttributeValue<EntityReference>("businessunitid");
                return new UserInfo
                {
                    Id = e.Id,
                    FullName = e.GetAttributeValue<string>("fullname") ?? "",
                    Email = e.GetAttributeValue<string>("internalemailaddress") ?? "",
                    BusinessUnitId = buRef?.Id ?? Guid.Empty,
                    BusinessUnitName = buRef?.Name ?? ""
                };
            }).ToList();
        }

        public static OperationResult AddUsersToTeam(IOrganizationService service, Guid teamId, string teamName, List<Guid> userIds, Action<string> log)
        {
            var result = new OperationResult();
            int added = 0;

            try
            {
                foreach (var userId in userIds)
                {
                    try
                    {
                        service.Execute(new AddMembersTeamRequest
                        {
                            TeamId = teamId,
                            MemberIds = new[] { userId }
                        });
                        added++;
                    }
                    catch (Exception ex)
                    {
                        log($"  Errore aggiunta utente {userId}: {ex.Message}");
                    }
                }

                result.Success = true;
                result.Message = $"{added}/{userIds.Count} utenti aggiunti al team '{teamName}'.";
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Errore: {ex.Message}";
            }

            return result;
        }

        public static OperationResult RemoveUsersFromTeam(IOrganizationService service, Guid teamId, string teamName, List<Guid> userIds, Action<string> log)
        {
            var result = new OperationResult();
            int removed = 0;

            try
            {
                foreach (var userId in userIds)
                {
                    try
                    {
                        service.Execute(new RemoveMembersTeamRequest
                        {
                            TeamId = teamId,
                            MemberIds = new[] { userId }
                        });
                        removed++;
                    }
                    catch (Exception ex)
                    {
                        log($"  Errore rimozione utente {userId}: {ex.Message}");
                    }
                }

                result.Success = true;
                result.Message = $"{removed}/{userIds.Count} utenti rimossi dal team '{teamName}'.";
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Errore: {ex.Message}";
            }

            return result;
        }

        // ─────────── Role Finder ───────────

        public static List<RoleCombinationResult> FindRoleCombinations(
            IOrganizationService service,
            List<PrivilegeRequirement> requirements,
            int maxRoles,
            Action<string> log)
        {
            if (requirements.Count == 0)
                return new List<RoleCombinationResult>();

            log("Risoluzione privilegi...");

            var reqRoleSets = new List<HashSet<string>>();
            var allRoleNames = new HashSet<string>();

            foreach (var req in requirements)
            {
                var privId = FindPrivilegeId(service, req.EntityLogicalName, req.AccessRight);
                if (privId == null)
                {
                    log($"  Privilegio non trovato: prv{req.AccessRight}{req.EntityLogicalName}");
                    log("  Verificare che l'entita' esista nell'ambiente.");
                    return new List<RoleCombinationResult>();
                }

                var roleNames = GetRoleNamesWithPrivilege(service, privId.Value, req.MinDepthMask);
                reqRoleSets.Add(roleNames);
                allRoleNames.UnionWith(roleNames);
                log($"  {req}: {roleNames.Count} ruoli");
            }

            if (allRoleNames.Count == 0)
            {
                log("Nessun ruolo candidato trovato.");
                return new List<RoleCombinationResult>();
            }

            // Build role -> satisfied requirements index
            var roleList = allRoleNames.OrderBy(n => n).ToList();
            var roleCoverage = new Dictionary<string, HashSet<int>>();
            foreach (var name in roleList)
            {
                var covered = new HashSet<int>();
                for (int i = 0; i < reqRoleSets.Count; i++)
                    if (reqRoleSets[i].Contains(name))
                        covered.Add(i);
                roleCoverage[name] = covered;
            }

            // Sort candidates by coverage (most-covering first)
            roleList = roleList.OrderByDescending(r => roleCoverage[r].Count).ToList();
            log($"Ruoli candidati: {roleList.Count}");

            int totalReqs = requirements.Count;
            var results = new List<RoleCombinationResult>();

            log("Ricerca combinazioni...");

            for (int size = 1; size <= Math.Min(maxRoles, roleList.Count); size++)
            {
                int maxCandidates = size switch { 1 => 500, 2 => 200, 3 => 80, 4 => 40, _ => 25 };
                var candidates = roleList.Count > maxCandidates
                    ? roleList.Take(maxCandidates).ToList()
                    : roleList;

                if (candidates.Count < roleList.Count)
                    log($"  Combinazioni di {size} (top {candidates.Count} ruoli)...");
                else
                    log($"  Combinazioni di {size}...");

                int found = 0;
                foreach (var combo in Combinations(candidates.Count, size))
                {
                    var covered = new HashSet<int>();
                    foreach (int idx in combo)
                        covered.UnionWith(roleCoverage[candidates[idx]]);

                    if (covered.Count == totalReqs)
                    {
                        results.Add(new RoleCombinationResult
                        {
                            RoleNames = combo.Select(i => candidates[i]).OrderBy(n => n).ToList()
                        });
                        found++;
                        if (found >= 200) break;
                    }
                }

                log($"    {found} combinazioni trovate");
                if (results.Count >= 500) break;
            }

            return results.OrderBy(r => r.Count).ThenBy(r => r.ToString()).ToList();
        }

        private static Guid? FindPrivilegeId(IOrganizationService service, string entityLogicalName, string accessRightName)
        {
            string privName = "prv" + accessRightName + entityLogicalName;
            var query = new QueryExpression("privilege")
            {
                ColumnSet = new ColumnSet("privilegeid"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("name", ConditionOperator.Equal, privName)
                    }
                },
                TopCount = 1
            };

            var result = service.RetrieveMultiple(query);
            return result.Entities.Count > 0 ? result.Entities[0].Id : (Guid?)null;
        }

        private static HashSet<string> GetRoleNamesWithPrivilege(
            IOrganizationService service, Guid privilegeId, int minDepth)
        {
            var query = new QueryExpression("role")
            {
                ColumnSet = new ColumnSet("name"),
                LinkEntities =
                {
                    new LinkEntity("role", "roleprivileges", "roleid", "roleid", JoinOperator.Inner)
                    {
                        LinkCriteria = new FilterExpression
                        {
                            Conditions =
                            {
                                new ConditionExpression("privilegeid", ConditionOperator.Equal, privilegeId),
                                new ConditionExpression("privilegedepthmask", ConditionOperator.GreaterEqual, minDepth)
                            }
                        }
                    }
                }
            };

            var names = new HashSet<string>();
            var results = service.RetrieveMultiple(query);
            foreach (var e in results.Entities)
            {
                var name = e.GetAttributeValue<string>("name");
                if (!string.IsNullOrEmpty(name))
                    names.Add(name);
            }
            return names;
        }

        private static IEnumerable<int[]> Combinations(int n, int k)
        {
            if (k > n || k <= 0) yield break;
            var c = new int[k];
            for (int i = 0; i < k; i++) c[i] = i;
            while (true)
            {
                yield return c;
                int pos = k - 1;
                while (pos >= 0 && c[pos] == n - k + pos) pos--;
                if (pos < 0) yield break;
                c[pos]++;
                for (int j = pos + 1; j < k; j++) c[j] = c[j - 1] + 1;
            }
        }
    }
}
