using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PSuite.Modules.Configuration.Core.Entities;

namespace PSuite.Modules.Configuration.Core.DAL.Configurations;

internal class HotelConfiguration : IEntityTypeConfiguration<Hotel>
{
    public void Configure(EntityTypeBuilder<Hotel> builder)
    {
        
    }
}
