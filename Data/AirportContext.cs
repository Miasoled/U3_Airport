using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using U3_Examen_Airport.Models;

namespace U3_Examen_Airport.Data;

public partial class AirportContext : DbContext
{
    public AirportContext(DbContextOptions<AirportContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Airline> Airlines { get; set; }

    public virtual DbSet<Airplane> Airplanes { get; set; }

    public virtual DbSet<AirplaneType> AirplaneTypes { get; set; }

    public virtual DbSet<Airport> Airports { get; set; }

    public virtual DbSet<AirportGeo> AirportGeos { get; set; }

    public virtual DbSet<AirportReachable> AirportReachables { get; set; }

    public virtual DbSet<Booking> Bookings { get; set; }

    public virtual DbSet<Employee> Employees { get; set; }

    public virtual DbSet<Flight> Flights { get; set; }

    public virtual DbSet<FlightLog> FlightLogs { get; set; }

    public virtual DbSet<Flightschedule> Flightschedules { get; set; }

    public virtual DbSet<Passenger> Passengers { get; set; }

    public virtual DbSet<Passengerdetail> Passengerdetails { get; set; }

    public virtual DbSet<Weatherdatum> Weatherdata { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasPostgresEnum("airportdb", "employee_department", new[] { "Marketing", "Buchhaltung", "Management", "Logistik", "Flugfeld" })
            .HasPostgresEnum("airportdb", "weatherdata_weather", new[] { "Nebel-Schneefall", "Schneefall", "Regen", "Regen-Schneefall", "Nebel-Regen", "Nebel-Regen-Gewitter", "Gewitter", "Nebel", "Regen-Gewitter" });

        modelBuilder.Entity<Airline>(entity =>
        {
            entity.HasKey(e => e.AirlineId).HasName("idx_360554_primary");

            entity.ToTable("airline", tb => tb.HasComment("Flughafen DB by Stefan Pröll, Eva Zangerle, Wolfgang Gassler is licensed under CC BY 4.0. To view a copy of this license, visit https://creativecommons.org/licenses/by/4.0"));

            entity.HasIndex(e => e.BaseAirport, "idx_360554_base_airport_idx");

            entity.HasIndex(e => e.Iata, "idx_360554_iata_unq").IsUnique();

            entity.Property(e => e.AirlineId).HasColumnName("airline_id");
            entity.Property(e => e.Airlinename)
                .HasMaxLength(30)
                .HasColumnName("airlinename");
            entity.Property(e => e.BaseAirport).HasColumnName("base_airport");
            entity.Property(e => e.Iata)
                .HasMaxLength(2)
                .IsFixedLength()
                .HasColumnName("iata");
        });

        modelBuilder.Entity<Airplane>(entity =>
        {
            entity.HasKey(e => e.AirplaneId).HasName("idx_360561_primary");

            entity.ToTable("airplane", tb => tb.HasComment("Flughafen DB by Stefan Pröll, Eva Zangerle, Wolfgang Gassler is licensed under CC BY 4.0. To view a copy of this license, visit https://creativecommons.org/licenses/by/4.0"));

            entity.HasIndex(e => e.TypeId, "idx_360561_type_id");

            entity.Property(e => e.AirplaneId)
                .ValueGeneratedNever()
                .HasColumnName("airplane_id");
            entity.Property(e => e.AirlineId).HasColumnName("airline_id");
            entity.Property(e => e.Capacity).HasColumnName("capacity");
            entity.Property(e => e.TypeId).HasColumnName("type_id");

            entity.HasOne(d => d.Type).WithMany(p => p.Airplanes)
                .HasForeignKey(d => d.TypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("airplane_ibfk_1");
        });

        modelBuilder.Entity<AirplaneType>(entity =>
        {
            entity.HasKey(e => e.TypeId).HasName("idx_360568_primary");

            entity.ToTable("airplane_type", tb => tb.HasComment("Flughafen DB by Stefan Pröll, Eva Zangerle, Wolfgang Gassler is licensed under CC BY 4.0. To view a copy of this license, visit https://creativecommons.org/licenses/by/4.0"));

            entity.Property(e => e.TypeId)
                .ValueGeneratedNever()
                .HasColumnName("type_id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Identifier)
                .HasMaxLength(50)
                .HasColumnName("identifier");
        });

        modelBuilder.Entity<Airport>(entity =>
        {
            entity.HasKey(e => e.AirportId).HasName("idx_360575_primary");

            entity.ToTable("airport", tb => tb.HasComment("Flughafen DB by Stefan Pröll, Eva Zangerle, Wolfgang Gassler is licensed under CC BY 4.0. To view a copy of this license, visit https://creativecommons.org/licenses/by/4.0"));

            entity.HasIndex(e => e.Iata, "idx_360575_iata_idx");

            entity.HasIndex(e => e.Icao, "idx_360575_icao_unq").IsUnique();

            entity.HasIndex(e => e.Name, "idx_360575_name_idx");

            entity.Property(e => e.AirportId).HasColumnName("airport_id");
            entity.Property(e => e.Iata)
                .HasMaxLength(3)
                .IsFixedLength()
                .HasColumnName("iata");
            entity.Property(e => e.Icao)
                .HasMaxLength(4)
                .IsFixedLength()
                .HasColumnName("icao");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("name");
        });

        modelBuilder.Entity<AirportGeo>(entity =>
        {
            entity.HasKey(e => e.AirportId).HasName("idx_360582_primary");

            entity.ToTable("airport_geo", tb => tb.HasComment("Flughafen DB by Stefan Pröll, Eva Zangerle, Wolfgang Gassler is licensed under CC BY 4.0. To view a copy of this license, visit https://creativecommons.org/licenses/by/4.0"));

            entity.HasIndex(e => e.Geolocation, "idx_360582_geolocation_spt").HasMethod("gist");

            entity.Property(e => e.AirportId)
                .ValueGeneratedNever()
                .HasColumnName("airport_id");
            entity.Property(e => e.City)
                .HasMaxLength(50)
                .HasColumnName("city");
            entity.Property(e => e.Country)
                .HasMaxLength(50)
                .HasColumnName("country");
            entity.Property(e => e.Geolocation).HasColumnName("geolocation");
            entity.Property(e => e.Latitude)
                .HasPrecision(11, 8)
                .HasColumnName("latitude");
            entity.Property(e => e.Longitude)
                .HasPrecision(11, 8)
                .HasColumnName("longitude");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("name");

            entity.HasOne(d => d.Airport).WithOne(p => p.AirportGeo)
                .HasForeignKey<AirportGeo>(d => d.AirportId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("airport_geo_ibfk_1");
        });

        modelBuilder.Entity<AirportReachable>(entity =>
        {
            entity.HasKey(e => e.AirportId).HasName("idx_360590_primary");

            entity.ToTable("airport_reachable", tb => tb.HasComment("Flughafen DB by Stefan Pröll, Eva Zangerle, Wolfgang Gassler is licensed under CC BY 4.0. To view a copy of this license, visit https://creativecommons.org/licenses/by/4.0"));

            entity.Property(e => e.AirportId)
                .ValueGeneratedNever()
                .HasColumnName("airport_id");
            entity.Property(e => e.Hops).HasColumnName("hops");

            entity.HasOne(d => d.Airport).WithOne(p => p.AirportReachable)
                .HasForeignKey<AirportReachable>(d => d.AirportId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("airport_reachable_ibfk_1");
        });

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(e => e.BookingId).HasName("idx_360594_primary");

            entity.ToTable("booking", tb => tb.HasComment("Flughafen DB by Stefan Pröll, Eva Zangerle, Wolfgang Gassler is licensed under CC BY 4.0. To view a copy of this license, visit https://creativecommons.org/licenses/by/4.0"));

            entity.HasIndex(e => e.FlightId, "idx_360594_flight_idx");

            entity.HasIndex(e => e.PassengerId, "idx_360594_passenger_idx");

            entity.HasIndex(e => new { e.FlightId, e.Seat }, "idx_360594_seatplan_unq").IsUnique();

            entity.Property(e => e.BookingId)
                .ValueGeneratedNever()
                .HasColumnName("booking_id");
            entity.Property(e => e.FlightId).HasColumnName("flight_id");
            entity.Property(e => e.PassengerId).HasColumnName("passenger_id");
            entity.Property(e => e.Price)
                .HasPrecision(10, 2)
                .HasColumnName("price");
            entity.Property(e => e.Seat)
                .HasMaxLength(4)
                .IsFixedLength()
                .HasColumnName("seat");

            entity.HasOne(d => d.Flight).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.FlightId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("booking_ibfk_1");
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.EmployeeId).HasName("idx_360601_primary");

            entity.ToTable("employee", tb => tb.HasComment("Flughafen DB by Stefan Pröll, Eva Zangerle, Wolfgang Gassler is licensed under CC BY 4.0. To view a copy of this license, visit https://creativecommons.org/licenses/by/4.0"));

            entity.HasIndex(e => e.Username, "idx_360601_user_unq").IsUnique();

            entity.Property(e => e.EmployeeId)
                .ValueGeneratedNever()
                .HasColumnName("employee_id");
            entity.Property(e => e.Birthdate).HasColumnName("birthdate");
            entity.Property(e => e.City)
                .HasMaxLength(100)
                .HasColumnName("city");
            entity.Property(e => e.Country)
                .HasMaxLength(100)
                .HasColumnName("country");
            entity.Property(e => e.Emailaddress)
                .HasMaxLength(120)
                .HasColumnName("emailaddress");
            entity.Property(e => e.Firstname)
                .HasMaxLength(100)
                .HasColumnName("firstname");
            entity.Property(e => e.Lastname)
                .HasMaxLength(100)
                .HasColumnName("lastname");
            entity.Property(e => e.Password)
                .HasMaxLength(32)
                .IsFixedLength()
                .HasColumnName("password");
            entity.Property(e => e.Salary)
                .HasPrecision(8, 2)
                .HasColumnName("salary");
            entity.Property(e => e.Sex)
                .HasMaxLength(1)
                .HasColumnName("sex");
            entity.Property(e => e.Street)
                .HasMaxLength(100)
                .HasColumnName("street");
            entity.Property(e => e.Telephoneno)
                .HasMaxLength(30)
                .HasColumnName("telephoneno");
            entity.Property(e => e.Username)
                .HasMaxLength(20)
                .HasColumnName("username");
            entity.Property(e => e.Zip).HasColumnName("zip");
        });

        modelBuilder.Entity<Flight>(entity =>
        {
            entity.HasKey(e => e.FlightId).HasName("idx_360614_primary");

            entity.ToTable("flight", tb => tb.HasComment("Flughafen DB by Stefan Pröll, Eva Zangerle, Wolfgang Gassler is licensed under CC BY 4.0. To view a copy of this license, visit https://creativecommons.org/licenses/by/4.0"));

            entity.HasIndex(e => e.AirlineId, "idx_360614_airline_idx");

            entity.HasIndex(e => e.AirplaneId, "idx_360614_airplane_idx");

            entity.HasIndex(e => e.Arrival, "idx_360614_arrivals_idx");

            entity.HasIndex(e => e.Departure, "idx_360614_departure_idx");

            entity.HasIndex(e => e.Flightno, "idx_360614_flightno");

            entity.HasIndex(e => e.From, "idx_360614_from_idx");

            entity.HasIndex(e => e.To, "idx_360614_to_idx");

            entity.Property(e => e.FlightId)
                .ValueGeneratedNever()
                .HasColumnName("flight_id");
            entity.Property(e => e.AirlineId).HasColumnName("airline_id");
            entity.Property(e => e.AirplaneId).HasColumnName("airplane_id");
            entity.Property(e => e.Arrival).HasColumnName("arrival");
            entity.Property(e => e.Departure).HasColumnName("departure");
            entity.Property(e => e.Flightno)
                .HasMaxLength(8)
                .IsFixedLength()
                .HasColumnName("flightno");
            entity.Property(e => e.From).HasColumnName("from");
            entity.Property(e => e.To).HasColumnName("to");
        });

        modelBuilder.Entity<FlightLog>(entity =>
        {
            entity.HasKey(e => e.FlightLogId).HasName("idx_360626_primary");

            entity.ToTable("flight_log", tb => tb.HasComment("Flughafen DB by Stefan Pröll, Eva Zangerle, Wolfgang Gassler is licensed under CC BY 4.0. To view a copy of this license, visit https://creativecommons.org/licenses/by/4.0"));

            entity.HasIndex(e => e.FlightId, "idx_360626_flight_log_ibfk_1");

            entity.Property(e => e.FlightLogId).HasColumnName("flight_log_id");
            entity.Property(e => e.AirlineIdNew).HasColumnName("airline_id_new");
            entity.Property(e => e.AirlineIdOld).HasColumnName("airline_id_old");
            entity.Property(e => e.AirplaneIdNew).HasColumnName("airplane_id_new");
            entity.Property(e => e.AirplaneIdOld).HasColumnName("airplane_id_old");
            entity.Property(e => e.ArrivalNew).HasColumnName("arrival_new");
            entity.Property(e => e.ArrivalOld).HasColumnName("arrival_old");
            entity.Property(e => e.Comment)
                .HasMaxLength(200)
                .HasColumnName("comment");
            entity.Property(e => e.DepartureNew).HasColumnName("departure_new");
            entity.Property(e => e.DepartureOld).HasColumnName("departure_old");
            entity.Property(e => e.FlightId).HasColumnName("flight_id");
            entity.Property(e => e.FlightnoNew)
                .HasMaxLength(8)
                .IsFixedLength()
                .HasColumnName("flightno_new");
            entity.Property(e => e.FlightnoOld)
                .HasMaxLength(8)
                .IsFixedLength()
                .HasColumnName("flightno_old");
            entity.Property(e => e.FromNew).HasColumnName("from_new");
            entity.Property(e => e.FromOld).HasColumnName("from_old");
            entity.Property(e => e.LogDate).HasColumnName("log_date");
            entity.Property(e => e.ToNew).HasColumnName("to_new");
            entity.Property(e => e.ToOld).HasColumnName("to_old");
            entity.Property(e => e.User)
                .HasMaxLength(100)
                .HasColumnName("user");
        });

        modelBuilder.Entity<Flightschedule>(entity =>
        {
            entity.HasKey(e => e.Flightno).HasName("idx_360648_primary");

            entity.ToTable("flightschedule", tb => tb.HasComment("Flughafen DB by Stefan Pröll, Eva Zangerle, Wolfgang Gassler is licensed under CC BY 4.0. To view a copy of this license, visit https://creativecommons.org/licenses/by/4.0"));

            entity.HasIndex(e => e.AirlineId, "idx_360648_airline_idx");

            entity.HasIndex(e => e.From, "idx_360648_from_idx");

            entity.HasIndex(e => e.To, "idx_360648_to_idx");

            entity.Property(e => e.Flightno)
                .HasMaxLength(8)
                .IsFixedLength()
                .HasColumnName("flightno");
            entity.Property(e => e.AirlineId).HasColumnName("airline_id");
            entity.Property(e => e.Arrival).HasColumnName("arrival");
            entity.Property(e => e.Departure).HasColumnName("departure");
            entity.Property(e => e.Friday)
                .HasDefaultValue(false)
                .HasColumnName("friday");
            entity.Property(e => e.From).HasColumnName("from");
            entity.Property(e => e.Monday)
                .HasDefaultValue(false)
                .HasColumnName("monday");
            entity.Property(e => e.Saturday)
                .HasDefaultValue(false)
                .HasColumnName("saturday");
            entity.Property(e => e.Sunday)
                .HasDefaultValue(false)
                .HasColumnName("sunday");
            entity.Property(e => e.Thursday)
                .HasDefaultValue(false)
                .HasColumnName("thursday");
            entity.Property(e => e.To).HasColumnName("to");
            entity.Property(e => e.Tuesday)
                .HasDefaultValue(false)
                .HasColumnName("tuesday");
            entity.Property(e => e.Wednesday)
                .HasDefaultValue(false)
                .HasColumnName("wednesday");
        });

        modelBuilder.Entity<Passenger>(entity =>
        {
            entity.HasKey(e => e.PassengerId).HasName("idx_360664_primary");

            entity.ToTable("passenger", tb => tb.HasComment("Flughafen DB by Stefan Pröll, Eva Zangerle, Wolfgang Gassler is licensed under CC BY 4.0. To view a copy of this license, visit https://creativecommons.org/licenses/by/4.0"));

            entity.HasIndex(e => e.Passportno, "idx_360664_pass_unq").IsUnique();

            entity.Property(e => e.PassengerId)
                .ValueGeneratedNever()
                .HasColumnName("passenger_id");
            entity.Property(e => e.Firstname)
                .HasMaxLength(100)
                .HasColumnName("firstname");
            entity.Property(e => e.Lastname)
                .HasMaxLength(100)
                .HasColumnName("lastname");
            entity.Property(e => e.Passportno)
                .HasMaxLength(9)
                .IsFixedLength()
                .HasColumnName("passportno");
        });

        modelBuilder.Entity<Passengerdetail>(entity =>
        {
            entity.HasKey(e => e.PassengerId).HasName("idx_360671_primary");

            entity.ToTable("passengerdetails", tb => tb.HasComment("Flughafen DB by Stefan Pröll, Eva Zangerle, Wolfgang Gassler is licensed under CC BY 4.0. To view a copy of this license, visit https://creativecommons.org/licenses/by/4.0"));

            entity.Property(e => e.PassengerId)
                .ValueGeneratedNever()
                .HasColumnName("passenger_id");
            entity.Property(e => e.Birthdate).HasColumnName("birthdate");
            entity.Property(e => e.City)
                .HasMaxLength(100)
                .HasColumnName("city");
            entity.Property(e => e.Country)
                .HasMaxLength(100)
                .HasColumnName("country");
            entity.Property(e => e.Emailaddress)
                .HasMaxLength(120)
                .HasColumnName("emailaddress");
            entity.Property(e => e.Sex)
                .HasMaxLength(1)
                .HasColumnName("sex");
            entity.Property(e => e.Street)
                .HasMaxLength(100)
                .HasColumnName("street");
            entity.Property(e => e.Telephoneno)
                .HasMaxLength(30)
                .HasColumnName("telephoneno");
            entity.Property(e => e.Zip).HasColumnName("zip");
        });

        modelBuilder.Entity<Weatherdatum>(entity =>
        {
            entity.HasKey(e => new { e.LogDate, e.Time, e.Station }).HasName("idx_360680_primary");

            entity.ToTable("weatherdata", tb => tb.HasComment("Flughafen DB by Stefan Pröll, Eva Zangerle, Wolfgang Gassler is licensed under CC BY 4.0. To view a copy of this license, visit https://creativecommons.org/licenses/by/4.0"));

            entity.Property(e => e.LogDate).HasColumnName("log_date");
            entity.Property(e => e.Time).HasColumnName("time");
            entity.Property(e => e.Station).HasColumnName("station");
            entity.Property(e => e.Airpressure)
                .HasPrecision(10, 2)
                .HasColumnName("airpressure");
            entity.Property(e => e.Humidity)
                .HasPrecision(4, 1)
                .HasColumnName("humidity");
            entity.Property(e => e.Temp)
                .HasPrecision(3, 1)
                .HasColumnName("temp");
            entity.Property(e => e.Wind)
                .HasPrecision(5, 2)
                .HasColumnName("wind");
            entity.Property(e => e.Winddirection).HasColumnName("winddirection");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
