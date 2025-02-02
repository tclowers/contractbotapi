﻿// <auto-generated />
using System;
using ContractBotApi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ContractBotApi.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    partial class ApplicationDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.7")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("ContractBotApi.Models.Contract", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Appendix")
                        .HasColumnType("text");

                    b.Property<string>("BlobStorageLocation")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("ContractText")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("ContractType")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("DeliveryTerms")
                        .HasColumnType("text");

                    b.Property<string>("OriginalFileName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Price")
                        .HasColumnType("text");

                    b.Property<string>("Product")
                        .HasColumnType("text");

                    b.Property<DateTime>("UploadTimestamp")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Volume")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("Contracts", (string)null);

                    b.UseTptMappingStrategy();
                });

            modelBuilder.Entity("ContractBotApi.Models.ConversationHistory", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Message")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Response")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTime>("Timestamp")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("UserId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("ConversationHistories");
                });

            modelBuilder.Entity("ContractBotApi.Models.ForwardContract", b =>
                {
                    b.HasBaseType("ContractBotApi.Models.Contract");

                    b.Property<string>("ForwardPrice")
                        .HasColumnType("text");

                    b.Property<DateTime?>("FutureDeliveryDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("SettlementTerms")
                        .HasColumnType("text");

                    b.ToTable("ForwardContracts", (string)null);
                });

            modelBuilder.Entity("ContractBotApi.Models.OptionContract", b =>
                {
                    b.HasBaseType("ContractBotApi.Models.Contract");

                    b.Property<DateTime?>("ExpirationDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("OptionType")
                        .HasColumnType("text");

                    b.Property<string>("StrikePrice")
                        .HasColumnType("text");

                    b.ToTable("OptionContracts", (string)null);
                });

            modelBuilder.Entity("ContractBotApi.Models.SpotContract", b =>
                {
                    b.HasBaseType("ContractBotApi.Models.Contract");

                    b.Property<bool>("ImmediateDelivery")
                        .HasColumnType("boolean");

                    b.Property<string>("PaymentMethod")
                        .HasColumnType("text");

                    b.Property<DateTime?>("SettlementDate")
                        .HasColumnType("timestamp with time zone");

                    b.ToTable("SpotContracts", (string)null);
                });

            modelBuilder.Entity("ContractBotApi.Models.SwapContract", b =>
                {
                    b.HasBaseType("ContractBotApi.Models.Contract");

                    b.Property<string>("NotionalAmount")
                        .HasColumnType("text");

                    b.Property<string>("PaymentFrequency")
                        .HasColumnType("text");

                    b.Property<string>("UnderlyingAsset")
                        .HasColumnType("text");

                    b.ToTable("SwapContracts", (string)null);
                });

            modelBuilder.Entity("ContractBotApi.Models.ForwardContract", b =>
                {
                    b.HasOne("ContractBotApi.Models.Contract", null)
                        .WithOne()
                        .HasForeignKey("ContractBotApi.Models.ForwardContract", "Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("ContractBotApi.Models.OptionContract", b =>
                {
                    b.HasOne("ContractBotApi.Models.Contract", null)
                        .WithOne()
                        .HasForeignKey("ContractBotApi.Models.OptionContract", "Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("ContractBotApi.Models.SpotContract", b =>
                {
                    b.HasOne("ContractBotApi.Models.Contract", null)
                        .WithOne()
                        .HasForeignKey("ContractBotApi.Models.SpotContract", "Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("ContractBotApi.Models.SwapContract", b =>
                {
                    b.HasOne("ContractBotApi.Models.Contract", null)
                        .WithOne()
                        .HasForeignKey("ContractBotApi.Models.SwapContract", "Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}
