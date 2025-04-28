using Microsoft.EntityFrameworkCore;
using jool_backend.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace jool_backend.Repository
{
    public class HashtagRepository
    {
        private readonly JoolContext _context;

        public HashtagRepository(JoolContext context)
        {
            _context = context;
        }

        public async Task<List<Hashtag>> GetAllHashtagsAsync()
        {
            return await _context.Hashtags.ToListAsync();
        }

        public async Task<Hashtag?> GetHashtagByIdAsync(int id)
        {
            return await _context.Hashtags.FindAsync(id);
        }

        public async Task<Hashtag?> GetHashtagByNameAsync(string name)
        {
            return await _context.Hashtags.FirstOrDefaultAsync(h => h.name == name);
        }

        public async Task<Hashtag> CreateHashtagAsync(Hashtag hashtag)
        {
            _context.Hashtags.Add(hashtag);
            await _context.SaveChangesAsync();
            return hashtag;
        }

        public async Task<bool> UpdateHashtagAsync(Hashtag hashtag)
        {
            _context.Entry(hashtag).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await HashtagExists(hashtag.hashtag_id))
                {
                    return false;
                }
                throw;
            }
        }

        public async Task<bool> DeleteHashtagAsync(int id)
        {
            var hashtag = await _context.Hashtags.FindAsync(id);
            if (hashtag == null)
            {
                return false;
            }

            _context.Hashtags.Remove(hashtag);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> HashtagExists(int id)
        {
            return await _context.Hashtags.AnyAsync(e => e.hashtag_id == id);
        }
    }
}
