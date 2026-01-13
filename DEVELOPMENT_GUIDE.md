# 👨‍💻 Development Guide

Guidelines untuk development dan contributing ke project ini

## Code Organization

### Backend Structure

```
PelindoCarLoan.API/
├── Controllers/          # API Endpoints
├── Models/              # Domain Models
├── DTOs/                # Data Transfer Objects
├── Services/            # Business Logic
├── Repositories/        # Data Access Layer
├── Middleware/          # Custom Middleware
├── Extensions/          # Extension Methods
├── Validators/          # FluentValidation
├── Helpers/             # Utility Functions
└── Migrations/          # Database Migrations
```

### Frontend Structure

```
src/
├── pages/               # Page Components (routes)
├── components/          # Reusable Components
├── layouts/             # Layout Components
├── services/            # API Services
├── store/               # Zustand State
├── types/               # TypeScript Definitions
├── lib/                 # Utilities
├── assets/              # Images, Fonts
└── styles/              # CSS/Tailwind
```

---

## Development Workflow

### 1. Get Latest Code

```bash
git clone <repo-url>
cd pelindo-car-loan
git checkout develop
git pull origin develop
```

### 2. Create Feature Branch

```bash
# Branch naming: feature/description or bugfix/description
git checkout -b feature/new-feature-name
```

### 3. Make Changes

```bash
# Backend
cd backend/PelindoCarLoan.API
# Make changes...

# Frontend
cd frontend
# Make changes...
```

### 4. Run Tests

```bash
# Backend tests
cd backend
dotnet test

# Frontend tests
cd frontend
npm test
```

### 5. Lint & Format

```bash
# Backend
cd backend/PelindoCarLoan.API
dotnet format

# Frontend
cd frontend
npm run lint
npm run lint -- --fix
```

### 6. Commit Changes

```bash
git add .
git commit -m "feat: add new feature"
# Commit message format: type: description
# Types: feat, fix, docs, style, refactor, test, chore
```

### 7. Push & Create PR

```bash
git push origin feature/new-feature-name
# Create Pull Request on GitHub
```

---

## Backend Development

### Creating a New Feature

#### Step 1: Create DTO

**File: DTOs/YourFeatureDtos.cs**

```csharp
public class CreateYourFeatureRequest
{
    public string Name { get; set; }
    public string Description { get; set; }
}

public class YourFeatureResponse
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

#### Step 2: Create Model

**File: Models/YourFeature.cs**

```csharp
public class YourFeature
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

#### Step 3: Create Repository

**File: Repositories/YourFeatureRepository.cs**

```csharp
public interface IYourFeatureRepository
{
    Task<YourFeature?> GetByIdAsync(int id);
    Task<IEnumerable<YourFeature>> GetAllAsync();
    Task<int> CreateAsync(YourFeature item);
    Task<bool> UpdateAsync(YourFeature item);
    Task<bool> DeleteAsync(int id);
}

public class YourFeatureRepository : IYourFeatureRepository
{
    private readonly IDbContext _dbContext;

    public YourFeatureRepository(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<YourFeature?> GetByIdAsync(int id)
    {
        const string sql = @"
            SELECT id, name, description, created_at
            FROM your_features
            WHERE id = :Id";

        using var connection = _dbContext.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<YourFeature>(sql, new { Id = id });
    }

    // Implement other methods...
}
```

#### Step 4: Create Service

**File: Services/YourFeatureService.cs**

```csharp
public interface IYourFeatureService
{
    Task<YourFeatureResponse?> GetByIdAsync(int id);
    Task<IEnumerable<YourFeatureResponse>> GetAllAsync();
    Task<int> CreateAsync(CreateYourFeatureRequest request);
    Task<bool> UpdateAsync(int id, UpdateYourFeatureRequest request);
    Task<bool> DeleteAsync(int id);
}

public class YourFeatureService : IYourFeatureService
{
    private readonly IYourFeatureRepository _repository;
    private readonly IMapper _mapper;

    public YourFeatureService(IYourFeatureRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<YourFeatureResponse?> GetByIdAsync(int id)
    {
        var item = await _repository.GetByIdAsync(id);
        return _mapper.Map<YourFeatureResponse>(item);
    }

    // Implement other methods...
}
```

#### Step 5: Create Controller

**File: Controllers/YourFeatureController.cs**

```csharp
[ApiController]
[Route("api/your-features")]
[Authorize]
public class YourFeatureController : ControllerBase
{
    private readonly IYourFeatureService _service;
    private readonly ILogger<YourFeatureController> _logger;

    public YourFeatureController(IYourFeatureService service, ILogger<YourFeatureController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null)
                return NotFound();

            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting feature");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(CreateYourFeatureRequest request)
    {
        try
        {
            // Validate request
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var id = await _service.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id }, new { success = true, data = id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating feature");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }
}
```

#### Step 6: Register Services

**File: Extensions/ServiceExtensions.cs**

```csharp
public static void ConfigureServices(this IServiceCollection services)
{
    services.AddScoped<IYourFeatureRepository, YourFeatureRepository>();
    services.AddScoped<IYourFeatureService, YourFeatureService>();
    // ... other services
}
```

#### Step 7: Add AutoMapper Profile

**File: Mappings/MappingProfile.cs**

```csharp
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<YourFeature, YourFeatureResponse>();
        CreateMap<CreateYourFeatureRequest, YourFeature>();
        CreateMap<UpdateYourFeatureRequest, YourFeature>();
    }
}
```

---

## Frontend Development

### Creating a New Page

#### Step 1: Create Types

**File: src/types/index.ts**

```typescript
export interface YourFeature {
  id: number;
  name: string;
  description: string;
  createdAt: string;
}

export interface CreateYourFeatureRequest {
  name: string;
  description: string;
}
```

#### Step 2: Create API Service

**File: src/services/yourFeatureService.ts**

```typescript
import api from './api';
import { YourFeature, CreateYourFeatureRequest } from '@/types';

export const yourFeatureService = {
  async getById(id: number) {
    const response = await api.get(`/your-features/${id}`);
    return response.data.data;
  },

  async getAll() {
    const response = await api.get('/your-features');
    return response.data.data;
  },

  async create(data: CreateYourFeatureRequest) {
    const response = await api.post('/your-features', data);
    return response.data.data;
  },

  async update(id: number, data: Partial<YourFeature>) {
    const response = await api.put(`/your-features/${id}`, data);
    return response.data.data;
  },

  async delete(id: number) {
    return await api.delete(`/your-features/${id}`);
  }
};
```

#### Step 3: Create Zustand Store (if needed)

**File: src/store/yourFeatureStore.ts**

```typescript
import { create } from 'zustand';
import { YourFeature } from '@/types';
import { yourFeatureService } from '@/services/yourFeatureService';

interface YourFeatureStore {
  features: YourFeature[];
  loading: boolean;
  error: string | null;
  
  fetchAll: () => Promise<void>;
  add: (feature: YourFeature) => void;
  remove: (id: number) => void;
}

export const useYourFeatureStore = create<YourFeatureStore>((set) => ({
  features: [],
  loading: false,
  error: null,

  fetchAll: async () => {
    set({ loading: true });
    try {
      const features = await yourFeatureService.getAll();
      set({ features, error: null });
    } catch (error) {
      set({ error: (error as Error).message });
    } finally {
      set({ loading: false });
    }
  },

  add: (feature) => set((state) => ({
    features: [...state.features, feature]
  })),

  remove: (id) => set((state) => ({
    features: state.features.filter(f => f.id !== id)
  }))
}));
```

#### Step 4: Create Components

**File: src/components/YourFeature/YourFeatureList.tsx**

```typescript
import React, { useEffect } from 'react';
import { useYourFeatureStore } from '@/store/yourFeatureStore';

export const YourFeatureList: React.FC = () => {
  const { features, loading, fetchAll } = useYourFeatureStore();

  useEffect(() => {
    fetchAll();
  }, [fetchAll]);

  if (loading) return <div>Loading...</div>;

  return (
    <div className="space-y-4">
      {features.map((feature) => (
        <div key={feature.id} className="p-4 border rounded">
          <h3 className="font-bold">{feature.name}</h3>
          <p>{feature.description}</p>
        </div>
      ))}
    </div>
  );
};
```

#### Step 5: Create Page

**File: src/pages/YourFeaturePage.tsx**

```typescript
import React from 'react';
import { YourFeatureList } from '@/components/YourFeature/YourFeatureList';
import MainLayout from '@/layouts/MainLayout';

export default function YourFeaturePage() {
  return (
    <MainLayout>
      <div className="max-w-6xl mx-auto p-6">
        <h1 className="text-3xl font-bold mb-6">Your Features</h1>
        <YourFeatureList />
      </div>
    </MainLayout>
  );
}
```

#### Step 6: Add Route

**File: src/App.tsx**

```typescript
import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import YourFeaturePage from '@/pages/YourFeaturePage';

function App() {
  return (
    <Router>
      <Routes>
        {/* ... other routes */}
        <Route path="/features" element={<YourFeaturePage />} />
      </Routes>
    </Router>
  );
}

export default App;
```

---

## Testing

### Backend Unit Tests

```csharp
[TestFixture]
public class YourFeatureServiceTests
{
    private IYourFeatureService _service;
    private Mock<IYourFeatureRepository> _repositoryMock;
    private Mock<IMapper> _mapperMock;

    [SetUp]
    public void Setup()
    {
        _repositoryMock = new Mock<IYourFeatureRepository>();
        _mapperMock = new Mock<IMapper>();
        _service = new YourFeatureService(_repositoryMock.Object, _mapperMock.Object);
    }

    [Test]
    public async Task GetByIdAsync_WithValidId_ReturnsFeature()
    {
        // Arrange
        int id = 1;
        var feature = new YourFeature { Id = id, Name = "Test" };
        var response = new YourFeatureResponse { Id = id, Name = "Test" };

        _repositoryMock.Setup(x => x.GetByIdAsync(id))
            .ReturnsAsync(feature);
        _mapperMock.Setup(x => x.Map<YourFeatureResponse>(feature))
            .Returns(response);

        // Act
        var result = await _service.GetByIdAsync(id);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(id, result.Id);
    }
}
```

### Frontend Component Tests

```typescript
import { render, screen } from '@testing-library/react';
import { YourFeatureList } from '@/components/YourFeature/YourFeatureList';

describe('YourFeatureList', () => {
  it('should render features', () => {
    render(<YourFeatureList />);
    expect(screen.getByRole('heading')).toBeInTheDocument();
  });
});
```

---

## Code Standards

### C# Coding Standards

```csharp
// 1. Naming Conventions
public class UserService { }              // PascalCase for classes
public interface IUserRepository { }      // PascalCase with I prefix for interfaces
private string _firstName;                // camelCase with _ prefix for private fields
public string FirstName { get; set; }     // PascalCase for properties
private async Task DoSomething() { }      // async methods end with Async

// 2. Method Signatures
public async Task<Result> DoSomethingAsync(int id, CancellationToken ct = default)
{
    // Implementation
}

// 3. Exception Handling
try
{
    // Code
}
catch (SpecificException ex)
{
    _logger.LogError(ex, "Specific error message with context");
    throw;
}
catch (Exception ex)
{
    _logger.LogError(ex, "Generic error occurred");
    throw new ApplicationException("User-friendly message", ex);
}

// 4. Documentation
/// <summary>
/// Gets a user by ID.
/// </summary>
/// <param name="id">The user ID</param>
/// <returns>User if found; null otherwise</returns>
public async Task<User?> GetUserByIdAsync(int id)
{
    // Implementation
}
```

### TypeScript/React Standards

```typescript
// 1. Component naming
interface UserCardProps {
  userId: number;
  onUserSelect: (id: number) => void;
}

export const UserCard: React.FC<UserCardProps> = ({ userId, onUserSelect }) => {
  return <div>User Card</div>;
};

// 2. Custom Hooks
export const useUserData = (id: number) => {
  const [user, setUser] = useState<User | null>(null);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    // Fetch logic
  }, [id]);

  return { user, loading };
};

// 3. Type definitions
type Status = 'idle' | 'loading' | 'success' | 'error';

interface ApiResponse<T> {
  success: boolean;
  data?: T;
  error?: string;
}

// 4. Event handlers
const handleSubmit = (e: React.FormEvent<HTMLFormElement>) => {
  e.preventDefault();
  // Handle form
};
```

---

## Git Workflow

### Commit Message Format

```
<type>(<scope>): <subject>

<body>

<footer>
```

Examples:
```
feat(auth): add JWT token refresh
fix(loan-request): prevent double submission
docs(readme): update installation steps
style(frontend): reformat header component
refactor(api): simplify approval logic
test(dashboard): add statistics tests
chore(deps): upgrade dependencies
```

### Branch Naming

- `feature/short-description` - New features
- `bugfix/short-description` - Bug fixes
- `hotfix/short-description` - Production fixes
- `docs/short-description` - Documentation
- `refactor/short-description` - Code improvements

### Pull Request Template

```markdown
## Description
Brief description of changes

## Type of Change
- [ ] Bug fix
- [ ] New feature
- [ ] Breaking change
- [ ] Documentation update

## Testing Done
- [ ] Unit tests
- [ ] Integration tests
- [ ] Manual testing

## Checklist
- [ ] Code follows style guide
- [ ] Self-review completed
- [ ] Comments added
- [ ] Documentation updated
```

---

## Performance Optimization

### Backend

```csharp
// 1. Async/Await
public async Task<User> GetUserAsync(int id)
{
    return await _repository.GetByIdAsync(id);
}

// 2. Lazy Loading / Pagination
public async Task<PagedResult<User>> GetUsersAsync(int pageNumber, int pageSize)
{
    // Implement pagination
}

// 3. Query Optimization
const string sql = @"
    SELECT u.id, u.name, u.email
    FROM users u
    WHERE u.status = 'ACTIVE'
    ORDER BY u.created_at DESC
    OFFSET :Offset ROWS FETCH NEXT :Limit ROWS ONLY";

// 4. Caching
[ResponseCache(Duration = 60)]
public async Task<List<User>> GetAllUsersAsync()
{
    return await _repository.GetAllAsync();
}
```

### Frontend

```typescript
// 1. React.memo for preventing re-renders
const UserCard = React.memo(({ user }: Props) => {
  return <div>{user.name}</div>;
});

// 2. useCallback to memoize functions
const handleClick = useCallback(() => {
  console.log('Clicked');
}, []);

// 3. useMemo for expensive computations
const expensiveData = useMemo(() => {
  return dataList.filter(x => x.status === 'active');
}, [dataList]);

// 4. Code splitting with React.lazy
const YourFeaturePage = React.lazy(() => import('./pages/YourFeaturePage'));
```

---

## Common Issues & Solutions

| Issue | Solution |
|-------|----------|
| "Cannot find module" | `npm install` or `dotnet restore` |
| Database not connecting | Check connection string in appsettings.json |
| Async deadlock | Use `.ConfigureAwait(false)` or `async/await` properly |
| CORS error | Add origin to `CorsSettings.AllowedOrigins` |
| State not updating | Check Zustand store subscription or React state |

---

## Version

Last Updated: January 13, 2026
