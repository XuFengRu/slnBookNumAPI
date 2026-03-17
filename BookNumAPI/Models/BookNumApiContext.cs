using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace BookNumAPI.Models;

public partial class BookNumApiContext : DbContext
{
    public BookNumApiContext()
    {
    }

    public BookNumApiContext(DbContextOptions<BookNumApiContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Account> Accounts { get; set; }

    public virtual DbSet<Activity> Activities { get; set; }

    public virtual DbSet<ActivityCategory> ActivityCategories { get; set; }

    public virtual DbSet<Chat> Chats { get; set; }

    public virtual DbSet<CityPreference> CityPreferences { get; set; }

    public virtual DbSet<Hobby> Hobbies { get; set; }

    public virtual DbSet<HobbyCategory> HobbyCategories { get; set; }

    public virtual DbSet<HobbyList> HobbyLists { get; set; }

    public virtual DbSet<Info> Infos { get; set; }

    public virtual DbSet<Liked> Likeds { get; set; }

    public virtual DbSet<Matched> Matcheds { get; set; }

    public virtual DbSet<Method> Methods { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderLog> OrderLogs { get; set; }

    public virtual DbSet<Participant> Participants { get; set; }

    public virtual DbSet<Premium> Premia { get; set; }

    public virtual DbSet<Renter> Renters { get; set; }

    public virtual DbSet<RenterPhoto> RenterPhotos { get; set; }

    public virtual DbSet<Review> Reviews { get; set; }

    public virtual DbSet<Service> Services { get; set; }

    public virtual DbSet<Token> Tokens { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Data Source=.;Initial Catalog=BookNumAPI;Integrated Security=True;Encrypt=False");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(e => e.AccountId).HasName("PK__Account__349DA5862ADDCBE4");

            entity.ToTable("Account");

            entity.Property(e => e.AccountId).HasColumnName("AccountID");
            entity.Property(e => e.Account1)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("Account");
            entity.Property(e => e.AuthProvider)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.AuthProviderId)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("AuthProviderID");
            entity.Property(e => e.CreateAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.IsEmailVerified).HasDefaultValue(false);
            entity.Property(e => e.LastLoginAt).HasColumnType("datetime");
            entity.Property(e => e.LastLoginIp)
                .HasMaxLength(45)
                .IsUnicode(false)
                .HasColumnName("LastLoginIP");
            entity.Property(e => e.Password).HasMaxLength(256);
            entity.Property(e => e.Role).HasDefaultValue(1);
            entity.Property(e => e.Status).HasDefaultValue(1);
            entity.Property(e => e.UpdateAt).HasColumnType("datetime");
        });

        modelBuilder.Entity<Activity>(entity =>
        {
            entity.HasKey(e => e.ActivityId).HasName("PK__Activity__45F4A7F1272FEF8C");

            entity.ToTable("Activity");

            entity.Property(e => e.ActivityId).HasColumnName("ActivityID");
            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.CreateAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.EndAt).HasColumnType("datetime");
            entity.Property(e => e.EventDate).HasColumnType("datetime");
            entity.Property(e => e.Image).HasMaxLength(500);
            entity.Property(e => e.Location).HasMaxLength(200);
            entity.Property(e => e.StartAt).HasColumnType("datetime");
            entity.Property(e => e.Title).HasMaxLength(50);
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.Category).WithMany(p => p.Activities)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Activity_Category");

            entity.HasOne(d => d.User).WithMany(p => p.Activities)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Activity_User");
        });

        modelBuilder.Entity<ActivityCategory>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK__Activity__19093A2BB741AC74");

            entity.ToTable("ActivityCategory");

            entity.HasIndex(e => e.CategoryName, "UQ__Activity__8517B2E0C7A91DEB").IsUnique();

            entity.HasIndex(e => e.CategoryName, "UQ__Activity__8517B2E0EEB13798").IsUnique();

            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.CategoryName).HasMaxLength(50);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<Chat>(entity =>
        {
            entity.HasKey(e => e.ChatId).HasName("PK__Chat__A9FBE626BC1F18E6");

            entity.ToTable("Chat");

            entity.Property(e => e.ChatId).HasColumnName("ChatID");
            entity.Property(e => e.MatchedId).HasColumnName("MatchedID");
            entity.Property(e => e.Message).HasMaxLength(4000);
            entity.Property(e => e.ReceiverId).HasColumnName("ReceiverID");
            entity.Property(e => e.SendAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.SenderId).HasColumnName("SenderID");

            entity.HasOne(d => d.Matched).WithMany(p => p.Chats)
                .HasForeignKey(d => d.MatchedId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Chat_Matched");

            entity.HasOne(d => d.Receiver).WithMany(p => p.ChatReceivers)
                .HasForeignKey(d => d.ReceiverId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Chat_Receiver");

            entity.HasOne(d => d.Sender).WithMany(p => p.ChatSenders)
                .HasForeignKey(d => d.SenderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Chat_Sender");
        });

        modelBuilder.Entity<CityPreference>(entity =>
        {
            entity.HasKey(e => e.CityPreferencesId).HasName("PK__CityPref__BB954C5A6EDCC5CA");

            entity.Property(e => e.CityPreferencesId).HasColumnName("CityPreferencesID");
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.User).WithMany(p => p.CityPreferences)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CityPref_User");
        });

        modelBuilder.Entity<Hobby>(entity =>
        {
            entity.HasKey(e => e.HobbyId).HasName("PK__Hobby__0ABE0BEFC270586B");

            entity.ToTable("Hobby");

            entity.Property(e => e.HobbyId).HasColumnName("HobbyID");
            entity.Property(e => e.HobbyCategoryId).HasColumnName("HobbyCategoryID");
            entity.Property(e => e.HobbyName).HasMaxLength(50);

            entity.HasOne(d => d.HobbyCategory).WithMany(p => p.Hobbies)
                .HasForeignKey(d => d.HobbyCategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Hobby_Category");
        });

        modelBuilder.Entity<HobbyCategory>(entity =>
        {
            entity.HasKey(e => e.HobbyCategoryId).HasName("PK__HobbyCat__4D6D66CB96B79E10");

            entity.ToTable("HobbyCategory");

            entity.Property(e => e.HobbyCategoryId).HasColumnName("HobbyCategoryID");
            entity.Property(e => e.CategoryName).HasMaxLength(100);
        });

        modelBuilder.Entity<HobbyList>(entity =>
        {
            entity.HasKey(e => e.HobbyListId).HasName("PK__HobbyLis__12197A4B28EA0910");

            entity.ToTable("HobbyList");

            entity.Property(e => e.HobbyListId).HasColumnName("HobbyListID");
            entity.Property(e => e.HobbyId).HasColumnName("HobbyID");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.Hobby).WithMany(p => p.HobbyLists)
                .HasForeignKey(d => d.HobbyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_HobbyList_Hobby");

            entity.HasOne(d => d.User).WithMany(p => p.HobbyLists)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_HobbyList_User");
        });

        modelBuilder.Entity<Info>(entity =>
        {
            entity.HasKey(e => e.InfoId).HasName("PK__Info__4DEC9D9A06EAE76E");

            entity.ToTable("Info");

            entity.Property(e => e.InfoId).HasColumnName("InfoID");
            entity.Property(e => e.Bio).HasMaxLength(600);
            entity.Property(e => e.CurrentCity).HasMaxLength(50);
            entity.Property(e => e.Height).HasColumnType("decimal(5, 1)");
            entity.Property(e => e.Job)
                .HasMaxLength(20)
                .IsFixedLength();
            entity.Property(e => e.Latitude).HasColumnType("decimal(9, 6)");
            entity.Property(e => e.Longitude).HasColumnType("decimal(9, 6)");
            entity.Property(e => e.Nickname).HasMaxLength(50);
            entity.Property(e => e.Photo).HasMaxLength(500);
            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.Weight).HasColumnType("decimal(5, 1)");

            entity.HasOne(d => d.User).WithMany(p => p.Infos)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Info_User");
        });

        modelBuilder.Entity<Liked>(entity =>
        {
            entity.HasKey(e => e.LikedId).HasName("PK__Liked__A7917838E26C9574");

            entity.ToTable("Liked");

            entity.Property(e => e.LikedId).HasColumnName("LikedID");
            entity.Property(e => e.CreateAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.LikedUserId).HasColumnName("LikedUserID");
            entity.Property(e => e.LikerUserId).HasColumnName("LikerUserID");

            entity.HasOne(d => d.LikedUser).WithMany(p => p.LikedLikedUsers)
                .HasForeignKey(d => d.LikedUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Liked_Liked");

            entity.HasOne(d => d.LikerUser).WithMany(p => p.LikedLikerUsers)
                .HasForeignKey(d => d.LikerUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Liked_Liker");
        });

        modelBuilder.Entity<Matched>(entity =>
        {
            entity.HasKey(e => e.MatchedId).HasName("PK__Matched__F618116B4FCC2B9D");

            entity.ToTable("Matched");

            entity.Property(e => e.MatchedId).HasColumnName("MatchedID");
            entity.Property(e => e.CreateAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.UnMatchAt).HasColumnType("datetime");
            entity.Property(e => e.User1Id).HasColumnName("User1ID");
            entity.Property(e => e.User2Id).HasColumnName("User2ID");

            entity.HasOne(d => d.User1).WithMany(p => p.MatchedUser1s)
                .HasForeignKey(d => d.User1Id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Matched_User1");

            entity.HasOne(d => d.User2).WithMany(p => p.MatchedUser2s)
                .HasForeignKey(d => d.User2Id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Matched_User2");
        });

        modelBuilder.Entity<Method>(entity =>
        {
            entity.HasKey(e => e.MethodId).HasName("PK__Method__FC681FB1FDD2702C");

            entity.ToTable("Method");

            entity.Property(e => e.MethodId).HasColumnName("MethodID");
            entity.Property(e => e.MethodName).HasMaxLength(100);
            entity.Property(e => e.PayPalId).HasColumnName("PayPalID");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("PK__Orders__C3905BAFCE519266");

            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.CreateAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.OrderNo).HasMaxLength(30);
            entity.Property(e => e.PayAt).HasColumnType("datetime");
            entity.Property(e => e.PayMethod).HasMaxLength(30);
            entity.Property(e => e.RenterId).HasColumnName("RenterID");
            entity.Property(e => e.ServiceId).HasColumnName("ServiceID");
            entity.Property(e => e.TransactionNo).HasMaxLength(100);
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.Renter).WithMany(p => p.Orders)
                .HasForeignKey(d => d.RenterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Orders_Renters");

            entity.HasOne(d => d.Service).WithMany(p => p.Orders)
                .HasForeignKey(d => d.ServiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Orders_Services");

            entity.HasOne(d => d.User).WithMany(p => p.Orders)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Orders_User");
        });

        modelBuilder.Entity<OrderLog>(entity =>
        {
            entity.HasKey(e => e.LogId).HasName("PK__OrderLog__5E5499A81A105201");

            entity.ToTable("OrderLog");

            entity.Property(e => e.LogId).HasColumnName("LogID");
            entity.Property(e => e.NewStatus).HasMaxLength(20);
            entity.Property(e => e.OldStatus).HasMaxLength(20);
            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.UpdateAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderLogs)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderHistory_Orders");
        });

        modelBuilder.Entity<Participant>(entity =>
        {
            entity.HasKey(e => e.ParticipantId).HasName("PK__Particip__7227997E8D08595A");

            entity.ToTable("Participant");

            entity.Property(e => e.ParticipantId).HasColumnName("ParticipantID");
            entity.Property(e => e.ActivityId).HasColumnName("ActivityID");
            entity.Property(e => e.CreateAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.UpdateAt).HasColumnType("datetime");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.Activity).WithMany(p => p.Participants)
                .HasForeignKey(d => d.ActivityId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Participant_Activity");

            entity.HasOne(d => d.User).WithMany(p => p.Participants)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Participant_User");
        });

        modelBuilder.Entity<Premium>(entity =>
        {
            entity.HasKey(e => e.PremiumId).HasName("PK__Premium__86B646E54FD4B798");

            entity.ToTable("Premium");

            entity.Property(e => e.PremiumId).HasColumnName("PremiumID");
            entity.Property(e => e.EndAt).HasColumnType("datetime");
            entity.Property(e => e.MethodId).HasColumnName("MethodID");
            entity.Property(e => e.StartAt).HasColumnType("datetime");
            entity.Property(e => e.SubscriptionId).HasColumnName("SubscriptionID");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.Method).WithMany(p => p.Premia)
                .HasForeignKey(d => d.MethodId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Premium_Method");

            entity.HasOne(d => d.User).WithMany(p => p.Premia)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Premium_User");
        });

        modelBuilder.Entity<Renter>(entity =>
        {
            entity.HasKey(e => e.RenterId).HasName("PK__Renters__921D6FFF1A311609");

            entity.Property(e => e.RenterId).HasColumnName("RenterID");
            entity.Property(e => e.CreateAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Gender)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.Height).HasColumnType("decimal(5, 1)");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.RenterName).HasMaxLength(50);
            entity.Property(e => e.StyleTag).HasMaxLength(100);
            entity.Property(e => e.Weight).HasColumnType("decimal(5, 1)");
        });

        modelBuilder.Entity<RenterPhoto>(entity =>
        {
            entity.HasKey(e => e.PhotoId).HasName("PK__RenterPh__21B7B5823FCE75E5");

            entity.Property(e => e.PhotoId).HasColumnName("PhotoID");
            entity.Property(e => e.RenterId).HasColumnName("RenterID");
            entity.Property(e => e.Url).HasMaxLength(500);

            entity.HasOne(d => d.Renter).WithMany(p => p.RenterPhotos)
                .HasForeignKey(d => d.RenterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RenterPhotos_Renters");
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(e => e.ReviewId).HasName("PK__Reviews__74BC79AE5A82892C");

            entity.Property(e => e.ReviewId).HasColumnName("ReviewID");
            entity.Property(e => e.Comment).HasMaxLength(2000);
            entity.Property(e => e.CreateAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.OrderId).HasColumnName("OrderID");

            entity.HasOne(d => d.Order).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Reviews_Orders");
        });

        modelBuilder.Entity<Service>(entity =>
        {
            entity.HasKey(e => e.ServiceId).HasName("PK__Services__C51BB0EA17A4391B");

            entity.Property(e => e.ServiceId).HasColumnName("ServiceID");
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.ServiceName).HasMaxLength(100);
        });

        modelBuilder.Entity<Token>(entity =>
        {
            entity.HasKey(e => e.TokenId).HasName("PK__Token__658FEEEAC489AB32");

            entity.ToTable("Token");

            entity.Property(e => e.CreateAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ExpiryAt).HasColumnType("datetime");
            entity.Property(e => e.Token1)
                .HasMaxLength(100)
                .HasColumnName("Token");
            entity.Property(e => e.UsedAt).HasColumnType("datetime");

            entity.HasOne(d => d.Account).WithMany(p => p.Tokens)
                .HasForeignKey(d => d.AccountId)
                .HasConstraintName("FK_UserTokens_Account");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__User__1788CCAC5462CFCC");

            entity.ToTable("User");

            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.AccountId).HasColumnName("AccountID");
            entity.Property(e => e.Gender)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.Name).HasMaxLength(50);
            entity.Property(e => e.Phone)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.Account).WithMany(p => p.Users)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_User_Account");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
