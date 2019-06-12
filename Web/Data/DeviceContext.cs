using Kakegurui.Web.Models.Device;
using Microsoft.EntityFrameworkCore;

namespace Kakegurui.Web.Data
{
    /// <summary>
    /// 设备和路口信息数据库
    /// </summary>
    public class DeviceContext : DbContext
    {
        public DbSet<TrafficRoad> Roads { get; set; }

        public DbSet<TrafficFlowDevice> FlowDevices { get; set; }

        public DbSet<TrafficFlowChannel> FlowChannels { get; set; }

        public DbSet<TrafficFlowLane> FlowLanes { get; set; }

        public DbSet<TrafficDensityDevice> DensityDevices { get; set; }

        public DbSet<TrafficDensityChannel> DensityChannels { get; set; }

        public DbSet<TrafficDensityRegion> DensityRegions { get; set; }


        public DeviceContext(DbContextOptions<DeviceContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TrafficRoad>()
                .ToTable("t_oms_road")
                .HasKey(r=>r.Id);

            modelBuilder.Entity<TrafficFlowDevice>()
                .ToTable("t_oms_flow")
                .HasKey(d=>d.Id);
            modelBuilder.Entity<TrafficFlowDevice>()
                .HasMany(f => f.Channels)
                .WithOne(c => c.FlowDevice)
                .HasForeignKey(c => c.DeviceId);

            modelBuilder.Entity<TrafficFlowChannel>()
                .ToTable("t_oms_flow_channel")
                .HasKey(c => new { c.DeviceId,c.ChannelIndex });
            modelBuilder.Entity<TrafficFlowChannel>()
                .HasMany(c => c.Lanes)
                .WithOne(l => l.Channel)
                .HasForeignKey(c =>new { c.DeviceId ,c.ChannelIndex})
                .HasConstraintName("ForeignKey_Channel_Lane"); 
            modelBuilder.Entity<TrafficFlowChannel>()
                .HasOne(c => c.Road)
                .WithMany(r => r.FlowChannels)
                .HasForeignKey(c=>c.RoadId);

            modelBuilder.Entity<TrafficFlowLane>()
                .ToTable("t_oms_flow_lane")
                .HasKey(c => new { c.LaneId });

            modelBuilder.Entity<TrafficDensityDevice>()
                .ToTable("t_oms_density")
                .HasKey(d => d.Id);
            modelBuilder.Entity<TrafficDensityDevice>()
                .HasMany(f => f.Channels)
                .WithOne(c => c.DensityDevice)
                .HasForeignKey(c => c.DeviceId);

            modelBuilder.Entity<TrafficDensityChannel>()
                .ToTable("t_oms_density_channel")
                .HasKey(c => new { c.DeviceId,c.ChannelIndex });
            modelBuilder.Entity<TrafficDensityChannel>()
                .HasMany(c => c.Regions)
                .WithOne(l => l.Channel)
                .HasForeignKey(c => new { c.DeviceId, c.ChannelIndex })
                .HasConstraintName("ForeignKey_Channel_Region"); 
            modelBuilder.Entity<TrafficDensityChannel>()
                .HasOne(c => c.Road)
                .WithMany(r => r.DensityChannels)
                .HasForeignKey(c => c.RoadId);

            modelBuilder.Entity<TrafficDensityRegion>()
                .ToTable("t_oms_density_region")
                .HasKey(r => new { r.RegionId });

        }
    }
}
