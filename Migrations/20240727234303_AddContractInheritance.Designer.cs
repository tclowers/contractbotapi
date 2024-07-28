﻿// <auto-generated />
using System;
using ContractBotApi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ContractBotApi.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20240727234303_AddContractInheritance")]
    partial class AddContractInheritance
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
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

                    b.Property<string>("Discriminator")
                        .IsRequired()
                        .HasMaxLength(21)
                        .HasColumnType("character varying(21)");

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

                    b.ToTable("Contracts");

                    b.HasDiscriminator().HasValue("Contract");

                    b.UseTphMappingStrategy();
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

                    b.HasDiscriminator().HasValue("ForwardContract");
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

                    b.HasDiscriminator().HasValue("OptionContract");
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

                    b.HasDiscriminator().HasValue("SpotContract");
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

                    b.HasDiscriminator().HasValue("SwapContract");
                });
#pragma warning restore 612, 618
        }
    }
}