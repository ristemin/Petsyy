# Petsy
We are building a .NET MVC app for a Veterinary client who wants to keep track of pets, their owners and which vaccines the pet has already taken.

# Steps implemented
### Using individual accounts when creating the program to implement authentication of users and enable authorization.
```<csharp>
// Adding Identity services
builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>();

app.UseAuthentication(); // Authentication middleware
app.UseAuthorization(); // Authorization middleware
```
      
### Create a database diagram and implement said diagram, creating needed models
  Models for:
  * Person (Id, Name, Surname, Age)
  * Pet (Id, Name, Description, Age)
  * Vaccine (Id, Name)
### Creating database with EF Core.
* Install correct EF Core Tools Version
* Establish relationships:
  + Person - Pet: One person can have multiple pets, one pet can have one person.
```<csharp>
    modelBuilder.Entity<Pet>()
     .HasOne(p => p.Person)
     .WithMany(o => o.Pets)
     .HasForeignKey(p => p.PersonId);
```
  + Pet - Vaccine: many to many; one pet can have multiple vaccines and vaccines can be associated with multiple pets
```<csharp>
 modelBuilder.Entity<Pet>()
     .HasMany(p => p.Vaccines)
     .WithMany(v => v.Pets);
```
    
### Scaffolding the Views for the Models with CRUD functionality.
1. Right click controllers folder
2. Add --> Controller
3. Select 'MVC Controller with Views, using Entity Framework'
4. Choosing a model class models above
5. Select the data context class as ApplicationDbContext
6. Create controller name and add
7. Repeat for all models.
   
### Implement caching: sliding and absolute expiration, cache removal when updating and deleting entities.
* Implemented caching logic in all controllers using IMemoryCache
  - Example from PeopleController
  
 - Inject constructor with ApplicationDbContext and an instance of IMemoryCache in order to be able to perform database operations and handle caching
public class PeopleController : Controller
```<csharp>
{
    private readonly ApplicationDbContext _context;
    private IMemoryCache _memoryCache;

    public PeopleController(ApplicationDbContext context, IMemoryCache memoryCache)
    {
        _context = context;
        _memoryCache = memoryCache;
    }
```
- With the Index action, the program is checking for data for "people" in cache. If it doesn't exist, it retrieves it from the database and sets up caching options. Then it caches the data and returns the view with the retrieved data.
 ```<csharp>
public async Task<IActionResult> Index()
  {
      List<Person> people;

      if (!_memoryCache.TryGetValue("people", out people))
      {
          people = await _context.People.ToListAsync();

          MemoryCacheEntryOptions cacheOptions = new MemoryCacheEntryOptions();
          cacheOptions.SetPriority(CacheItemPriority.Low);
          cacheOptions.SetSlidingExpiration(new TimeSpan(0, 0, 15));
          cacheOptions.SetAbsoluteExpiration(new TimeSpan(0, 0, 30));

          _memoryCache.Set("people", people, cacheOptions);
      }

      return View(people);
  }
```

- In the Edit and DeleteCOnfirmed actions, the cache entry is removed with the following code
```<csharp>
 _memoryCache.Remove("people");
 ```

### Adding roles and authorization
- Added "Admin" and "User" roles in SQL
- Connected "Admin" with the admin user and the other users with "User"

# Steps left to be implemented
1. Being able to select multiple vaccines and showing all of them in the pets details.
2. Adding a Unit Test Project.
