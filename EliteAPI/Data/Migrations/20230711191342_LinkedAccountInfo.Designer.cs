﻿// <auto-generated />
using System;
using System.Collections.Generic;
using System.Text.Json;
using EliteAPI.Data;
using EliteAPI.Models.Entities;
using EliteAPI.Models.Entities.Accounts;
using EliteAPI.Models.Entities.Hypixel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EliteAPI.Data.Migrations
{
    [DbContext(typeof(DataContext))]
    [Migration("20230711191342_LinkedAccountInfo")]
    partial class LinkedAccountInfo
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "7.0.8")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("EliteAPI.Models.Entities.AccountEntities", b =>
                {
                    b.Property<decimal>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("numeric(20,0)");

                    b.Property<string>("Avatar")
                        .HasColumnType("text");

                    b.Property<string>("Discriminator")
                        .HasColumnType("text");

                    b.Property<string>("DisplayName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Email")
                        .HasColumnType("text");

                    b.Property<EliteInventory>("Inventory")
                        .IsRequired()
                        .HasColumnType("jsonb");

                    b.Property<string>("Locale")
                        .HasColumnType("text");

                    b.Property<int>("Permissions")
                        .HasColumnType("integer");

                    b.Property<List<Purchase>>("Purchases")
                        .IsRequired()
                        .HasColumnType("jsonb");

                    b.Property<List<Redemption>>("Redemptions")
                        .IsRequired()
                        .HasColumnType("jsonb");

                    b.Property<EliteSettings>("Settings")
                        .IsRequired()
                        .HasColumnType("jsonb");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("Accounts");
                });

            modelBuilder.Entity("EliteAPI.Models.Entities.FarmingWeight", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<Dictionary<string, double>>("BonusWeight")
                        .IsRequired()
                        .HasColumnType("jsonb");

                    b.Property<Dictionary<string, double>>("CropWeight")
                        .IsRequired()
                        .HasColumnType("jsonb");

                    b.Property<Guid>("ProfileMemberId")
                        .HasColumnType("uuid");

                    b.Property<double>("TotalWeight")
                        .HasColumnType("double precision");

                    b.HasKey("Id");

                    b.HasIndex("ProfileMemberId")
                        .IsUnique();

                    b.ToTable("FarmingWeights");
                });

            modelBuilder.Entity("EliteAPI.Models.Entities.Hypixel.ContestParticipation", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<int>("Collected")
                        .HasColumnType("integer");

                    b.Property<long>("JacobContestId")
                        .HasColumnType("bigint");

                    b.Property<int?>("JacobDataId")
                        .HasColumnType("integer");

                    b.Property<int>("MedalEarned")
                        .HasColumnType("integer");

                    b.Property<int>("Position")
                        .HasColumnType("integer");

                    b.Property<Guid>("ProfileMemberId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("JacobContestId");

                    b.HasIndex("JacobDataId");

                    b.HasIndex("ProfileMemberId");

                    b.ToTable("ContestParticipations");
                });

            modelBuilder.Entity("EliteAPI.Models.Entities.Hypixel.JacobContest", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                    b.Property<int>("Crop")
                        .HasColumnType("integer");

                    b.Property<int>("Participants")
                        .HasColumnType("integer");

                    b.Property<long>("Timestamp")
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.HasIndex("Timestamp");

                    b.ToTable("JacobContests");
                });

            modelBuilder.Entity("EliteAPI.Models.Entities.Hypixel.JacobData", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<long>("ContestsLastUpdated")
                        .HasColumnType("bigint");

                    b.Property<int>("Participations")
                        .HasColumnType("integer");

                    b.Property<Guid>("ProfileMemberId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("ProfileMemberId")
                        .IsUnique();

                    b.ToTable("JacobData");
                });

            modelBuilder.Entity("EliteAPI.Models.Entities.Hypixel.PlayerData", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("DisplayName")
                        .HasColumnType("text");

                    b.Property<long>("FirstLogin")
                        .HasColumnType("bigint");

                    b.Property<int>("Karma")
                        .HasColumnType("integer");

                    b.Property<long>("LastLogin")
                        .HasColumnType("bigint");

                    b.Property<long>("LastLogout")
                        .HasColumnType("bigint");

                    b.Property<long>("LastUpdated")
                        .HasColumnType("bigint");

                    b.Property<string>("MonthlyPackageRank")
                        .HasColumnType("text");

                    b.Property<string>("MonthlyRankColor")
                        .HasColumnType("text");

                    b.Property<string>("MostRecentMonthlyPackageRank")
                        .HasColumnType("text");

                    b.Property<double>("NetworkExp")
                        .HasColumnType("double precision");

                    b.Property<string>("NewPackageRank")
                        .HasColumnType("text");

                    b.Property<string>("Rank")
                        .HasColumnType("text");

                    b.Property<string>("RankPlusColor")
                        .HasColumnType("text");

                    b.Property<int>("RewardHighScore")
                        .HasColumnType("integer");

                    b.Property<int>("RewardScore")
                        .HasColumnType("integer");

                    b.Property<int>("RewardStreak")
                        .HasColumnType("integer");

                    b.Property<int>("TotalDailyRewards")
                        .HasColumnType("integer");

                    b.Property<int>("TotalRewards")
                        .HasColumnType("integer");

                    b.Property<string>("Uuid")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("Uuid")
                        .IsUnique();

                    b.ToTable("PlayerData");
                });

            modelBuilder.Entity("EliteAPI.Models.Entities.Hypixel.Profile", b =>
                {
                    b.Property<string>("ProfileId")
                        .HasColumnType("text");

                    b.Property<double>("BankBalance")
                        .HasColumnType("double precision");

                    b.Property<Dictionary<string, int>>("CraftedMinions")
                        .IsRequired()
                        .HasColumnType("jsonb");

                    b.Property<string>("GameMode")
                        .HasColumnType("text");

                    b.Property<bool>("IsDeleted")
                        .HasColumnType("boolean");

                    b.Property<long>("LastUpdated")
                        .HasColumnType("bigint");

                    b.Property<string>("ProfileName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("ProfileId");

                    b.ToTable("Profiles");
                });

            modelBuilder.Entity("EliteAPI.Models.Entities.Hypixel.ProfileMember", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<Dictionary<string, int>>("CollectionTiers")
                        .IsRequired()
                        .HasColumnType("jsonb");

                    b.Property<JsonDocument>("Collections")
                        .IsRequired()
                        .HasColumnType("jsonb");

                    b.Property<Dictionary<string, int>>("Essence")
                        .IsRequired()
                        .HasColumnType("jsonb");

                    b.Property<bool>("IsSelected")
                        .HasColumnType("boolean");

                    b.Property<long>("LastUpdated")
                        .HasColumnType("bigint");

                    b.Property<List<Pet>>("Pets")
                        .IsRequired()
                        .HasColumnType("jsonb");

                    b.Property<string>("PlayerUuid")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("ProfileId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<double>("Purse")
                        .HasColumnType("double precision");

                    b.Property<int>("SkyblockXp")
                        .HasColumnType("integer");

                    b.Property<Dictionary<string, double>>("Stats")
                        .IsRequired()
                        .HasColumnType("jsonb");

                    b.Property<bool>("WasRemoved")
                        .HasColumnType("boolean");

                    b.HasKey("Id");

                    b.HasIndex("PlayerUuid");

                    b.HasIndex("ProfileId");

                    b.ToTable("ProfileMembers");
                });

            modelBuilder.Entity("EliteAPI.Models.Entities.Hypixel.Skills", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<double>("Alchemy")
                        .HasColumnType("double precision");

                    b.Property<double>("Carpentry")
                        .HasColumnType("double precision");

                    b.Property<double>("Combat")
                        .HasColumnType("double precision");

                    b.Property<double>("Enchanting")
                        .HasColumnType("double precision");

                    b.Property<double>("Farming")
                        .HasColumnType("double precision");

                    b.Property<double>("Fishing")
                        .HasColumnType("double precision");

                    b.Property<double>("Foraging")
                        .HasColumnType("double precision");

                    b.Property<double>("Mining")
                        .HasColumnType("double precision");

                    b.Property<Guid>("ProfileMemberId")
                        .HasColumnType("uuid");

                    b.Property<double>("Runecrafting")
                        .HasColumnType("double precision");

                    b.Property<double>("Social")
                        .HasColumnType("double precision");

                    b.Property<double>("Taming")
                        .HasColumnType("double precision");

                    b.HasKey("Id");

                    b.HasIndex("ProfileMemberId")
                        .IsUnique();

                    b.ToTable("Skills");
                });

            modelBuilder.Entity("EliteAPI.Models.Entities.MinecraftAccount", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("text");

                    b.Property<decimal?>("AccountEntitiesId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal?>("AccountId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<long>("LastUpdated")
                        .HasColumnType("bigint");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<List<MinecraftAccountProperty>>("Properties")
                        .IsRequired()
                        .HasColumnType("jsonb");

                    b.Property<bool>("Selected")
                        .HasColumnType("boolean");

                    b.HasKey("Id");

                    b.HasIndex("AccountEntitiesId");

                    b.ToTable("MinecraftAccounts");
                });

            modelBuilder.Entity("EliteAPI.Models.Entities.FarmingWeight", b =>
                {
                    b.HasOne("EliteAPI.Models.Entities.Hypixel.ProfileMember", "ProfileMember")
                        .WithOne("FarmingWeight")
                        .HasForeignKey("EliteAPI.Models.Entities.FarmingWeight", "ProfileMemberId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("ProfileMember");
                });

            modelBuilder.Entity("EliteAPI.Models.Entities.Hypixel.ContestParticipation", b =>
                {
                    b.HasOne("EliteAPI.Models.Entities.Hypixel.JacobContest", "JacobContest")
                        .WithMany()
                        .HasForeignKey("JacobContestId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("EliteAPI.Models.Entities.Hypixel.JacobData", null)
                        .WithMany("Contests")
                        .HasForeignKey("JacobDataId");

                    b.HasOne("EliteAPI.Models.Entities.Hypixel.ProfileMember", "ProfileMember")
                        .WithMany()
                        .HasForeignKey("ProfileMemberId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("JacobContest");

                    b.Navigation("ProfileMember");
                });

            modelBuilder.Entity("EliteAPI.Models.Entities.Hypixel.JacobData", b =>
                {
                    b.HasOne("EliteAPI.Models.Entities.Hypixel.ProfileMember", "ProfileMember")
                        .WithOne("JacobData")
                        .HasForeignKey("EliteAPI.Models.Entities.Hypixel.JacobData", "ProfileMemberId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.OwnsOne("EliteAPI.Models.Entities.Hypixel.MedalInventory", "EarnedMedals", b1 =>
                        {
                            b1.Property<int>("JacobDataId")
                                .HasColumnType("integer");

                            b1.Property<int>("Bronze")
                                .HasColumnType("integer");

                            b1.Property<int>("Gold")
                                .HasColumnType("integer");

                            b1.Property<int>("Silver")
                                .HasColumnType("integer");

                            b1.HasKey("JacobDataId");

                            b1.ToTable("JacobData");

                            b1.WithOwner()
                                .HasForeignKey("JacobDataId");
                        });

                    b.OwnsOne("EliteAPI.Models.Entities.Hypixel.MedalInventory", "Medals", b1 =>
                        {
                            b1.Property<int>("JacobDataId")
                                .HasColumnType("integer");

                            b1.Property<int>("Bronze")
                                .HasColumnType("integer");

                            b1.Property<int>("Gold")
                                .HasColumnType("integer");

                            b1.Property<int>("Silver")
                                .HasColumnType("integer");

                            b1.HasKey("JacobDataId");

                            b1.ToTable("JacobData");

                            b1.WithOwner()
                                .HasForeignKey("JacobDataId");
                        });

                    b.OwnsOne("EliteAPI.Models.Entities.Hypixel.JacobPerks", "Perks", b1 =>
                        {
                            b1.Property<int>("JacobDataId")
                                .HasColumnType("integer");

                            b1.Property<int>("DoubleDrops")
                                .HasColumnType("integer");

                            b1.Property<int>("LevelCap")
                                .HasColumnType("integer");

                            b1.HasKey("JacobDataId");

                            b1.ToTable("JacobData");

                            b1.WithOwner()
                                .HasForeignKey("JacobDataId");
                        });

                    b.Navigation("EarnedMedals")
                        .IsRequired();

                    b.Navigation("Medals")
                        .IsRequired();

                    b.Navigation("Perks")
                        .IsRequired();

                    b.Navigation("ProfileMember");
                });

            modelBuilder.Entity("EliteAPI.Models.Entities.Hypixel.PlayerData", b =>
                {
                    b.HasOne("EliteAPI.Models.Entities.MinecraftAccount", "MinecraftAccount")
                        .WithOne("PlayerData")
                        .HasForeignKey("EliteAPI.Models.Entities.Hypixel.PlayerData", "Uuid")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.OwnsOne("EliteAPI.Models.Entities.Hypixel.SocialMediaLinks", "SocialMedia", b1 =>
                        {
                            b1.Property<int>("PlayerDataId")
                                .HasColumnType("integer");

                            b1.Property<string>("Discord")
                                .HasColumnType("text");

                            b1.Property<string>("Hypixel")
                                .HasColumnType("text");

                            b1.Property<string>("Youtube")
                                .HasColumnType("text");

                            b1.HasKey("PlayerDataId");

                            b1.ToTable("PlayerData");

                            b1.WithOwner()
                                .HasForeignKey("PlayerDataId");
                        });

                    b.Navigation("MinecraftAccount");

                    b.Navigation("SocialMedia")
                        .IsRequired();
                });

            modelBuilder.Entity("EliteAPI.Models.Entities.Hypixel.ProfileMember", b =>
                {
                    b.HasOne("EliteAPI.Models.Entities.MinecraftAccount", "MinecraftAccount")
                        .WithMany()
                        .HasForeignKey("PlayerUuid")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("EliteAPI.Models.Entities.Hypixel.Profile", "Profile")
                        .WithMany("Members")
                        .HasForeignKey("ProfileId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("MinecraftAccount");

                    b.Navigation("Profile");
                });

            modelBuilder.Entity("EliteAPI.Models.Entities.Hypixel.Skills", b =>
                {
                    b.HasOne("EliteAPI.Models.Entities.Hypixel.ProfileMember", "ProfileMember")
                        .WithOne("Skills")
                        .HasForeignKey("EliteAPI.Models.Entities.Hypixel.Skills", "ProfileMemberId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("ProfileMember");
                });

            modelBuilder.Entity("EliteAPI.Models.Entities.MinecraftAccount", b =>
                {
                    b.HasOne("EliteAPI.Models.Entities.AccountEntities", null)
                        .WithMany("MinecraftAccounts")
                        .HasForeignKey("AccountEntitiesId");
                });

            modelBuilder.Entity("EliteAPI.Models.Entities.AccountEntities", b =>
                {
                    b.Navigation("MinecraftAccounts");
                });

            modelBuilder.Entity("EliteAPI.Models.Entities.Hypixel.JacobData", b =>
                {
                    b.Navigation("Contests");
                });

            modelBuilder.Entity("EliteAPI.Models.Entities.Hypixel.Profile", b =>
                {
                    b.Navigation("Members");
                });

            modelBuilder.Entity("EliteAPI.Models.Entities.Hypixel.ProfileMember", b =>
                {
                    b.Navigation("FarmingWeight")
                        .IsRequired();

                    b.Navigation("JacobData")
                        .IsRequired();

                    b.Navigation("Skills")
                        .IsRequired();
                });

            modelBuilder.Entity("EliteAPI.Models.Entities.MinecraftAccount", b =>
                {
                    b.Navigation("PlayerData");
                });
#pragma warning restore 612, 618
        }
    }
}
