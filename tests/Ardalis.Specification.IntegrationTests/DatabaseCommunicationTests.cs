using Ardalis.Specification.IntegrationTests.SampleClient;
using Ardalis.Specification.IntegrationTests.SampleSpecs;
using Dapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Xunit;


namespace Ardalis.Specification.IntegrationTests
{
    public class DatabaseCommunicationTests
    {
        // Run EF Migrations\DBUp script to prepare database before running your tests.
        const string ConnectionString = "Data Source=database;Initial Catalog=SampleDatabase;PersistSecurityInfo=True;User ID=sa;Password=P@ssW0rd!";
        public SampleDbContext _dbContext;
        public EfRepository<Blog> _blogRepository;
        public EfRepository<Post> _postRepository;

        public DatabaseCommunicationTests()
        {
            var optionsBuilder = new DbContextOptionsBuilder<SampleDbContext>();
            optionsBuilder.UseSqlServer(ConnectionString);
            _dbContext = new SampleDbContext(optionsBuilder.Options);

            // Run this if you've made seed data or schema changes to force the container to rebuild the db
            // _dbContext.Database.EnsureDeleted();

            // Note: If the database exists, this will do nothing, so it only creates it once.
            // This is fine since these tests all perform read-only operations
            _dbContext.Database.EnsureCreated();

            _blogRepository = new EfRepository<Blog>(_dbContext);
            _postRepository = new EfRepository<Post>(_dbContext);
        }

        [Fact]
        public async Task CanConnectAndRunQuery()
        {
            using (var conn = new SqlConnection(ConnectionString))
            {
                await conn.OpenAsync();
                const string query = "SELECT 1 AS Data";
                var result = (await conn.QueryAsync<int>(query)).FirstOrDefault();
                result.Should().Be(1);
            }
        }

        [Fact]
        public async Task GetBlogUsingEF()
        {
            var result = _dbContext.Blogs.FirstOrDefault();

            Assert.Equal(BlogBuilder.VALID_BLOG_ID, result.Id);
        }

        [Fact]
        public async Task GetBlogUsingEFRepository()
        {
            var result = await _blogRepository.GetByIdAsync(BlogBuilder.VALID_BLOG_ID);

            result.Should().NotBeNull();
            result.Name.Should().Be(BlogBuilder.VALID_BLOG_NAME);
            result.Posts.Count.Should().Be(0);
        }

        [Fact]
        public async Task GetBlogUsingEFRepositoryAndSpecShouldIncludePosts()
        {
            var result = (await _blogRepository.ListAsync(new BlogWithPostsSpec(BlogBuilder.VALID_BLOG_ID))).SingleOrDefault();

            result.Should().NotBeNull();
            result.Name.Should().Be(BlogBuilder.VALID_BLOG_NAME);
            result.Posts.Count.Should().BeGreaterThan(100);
        }

        [Fact]
        public async Task GetBlogUsingEFRepositoryAndSpecWithStringIncludeShouldIncludePosts()
        {
            var result = (await _blogRepository.ListAsync(new BlogWithPostsUsingStringSpec(BlogBuilder.VALID_BLOG_ID))).SingleOrDefault();

            result.Should().NotBeNull();
            result.Name.Should().Be(BlogBuilder.VALID_BLOG_NAME);
            result.Posts.Count.Should().BeGreaterThan(100);
        }

        [Fact]
        public async Task GetSecondPageOfPostsUsingPostsByBlogPaginatedSpec()
        {
            int pageSize = 10;
            int pageIndex = 1; // page 2
            var result = (await _postRepository.ListAsync(new PostsByBlogPaginatedSpec(pageIndex * pageSize, pageSize, BlogBuilder.VALID_BLOG_ID))).ToList();

            result.Count.Should().Be(pageSize);
            result.First().Id.Should().Be(309);
            result.Last().Id.Should().Be(318);
        }

        [Fact]
        public async Task GetPostsWithOrderedSpec()
        {
            var result = (await _postRepository.ListAsync(new PostsByBlogOrderedSpec(BlogBuilder.VALID_BLOG_ID))).ToList();

            result.First().Id.Should().Be(234);
            result.Last().Id.Should().Be(399);
        }

        [Fact]
        public async Task GetPostsWithOrderedSpecDescending()
        {
            var result = (await _postRepository.ListAsync(new PostsByBlogOrderedSpec(BlogBuilder.VALID_BLOG_ID, false))).ToList();

            result.First().Id.Should().Be(399);
            result.Last().Id.Should().Be(234);
        }

        // TODO: This could move to the Unit Tests project if specs were in separate project
        [Fact]
        public async Task EnableCacheShouldSetCacheKeyProperly()
        {
            var spec = new BlogWithPostsSpec(BlogBuilder.VALID_BLOG_ID);

            spec.CacheKey.Should().Be($"BlogWithPostsSpec-{BlogBuilder.VALID_BLOG_ID}");
        }

        [Fact]
        public async Task GroupByShouldWorkProperlyl()
        {
            var spec = new PostsGroupedByIdSpec();
            var result = (await _postRepository.ListAsync(spec)).ToList();

            result.First().Id.Should().Be(301);
            result.Skip(1).Take(1).First().Id.Should().Be(303);
        }
    }
}
