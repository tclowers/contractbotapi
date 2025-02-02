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
    [Migration("20240727195059_AddContractTextField")]
    partial class AddContractTextField
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

                    b.Property<string>("OriginalFileName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<decimal?>("Price")
                        .HasColumnType("numeric");

                    b.Property<string>("Product")
                        .HasColumnType("text");

                    b.Property<DateTime>("UploadTimestamp")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int?>("Volume")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.ToTable("Contracts");
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
#pragma warning restore 612, 618
        }
    }
}
