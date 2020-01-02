namespace Cinema.DataProcessor
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml.Serialization;
    using Cinema.Data.Models;
    using Cinema.DataProcessor.ImportDto;
    using Data;
    using Microsoft.EntityFrameworkCore;
    using Newtonsoft.Json;

    public static class Deserializer
    {
        private const string ErrorMessage = "Invalid data!";
        private const string SuccessfulImportMovie
            = "Successfully imported {0} with genre {1} and rating {2}!";
        private const string SuccessfulImportHallSeat
            = "Successfully imported {0}({1}) with {2} seats!";
        private const string SuccessfulImportProjection
            = "Successfully imported projection {0} on {1}!";
        private const string SuccessfulImportCustomerTicket
            = "Successfully imported customer {0} {1} with bought tickets: {2}!";

        public static string ImportMovies(CinemaContext context, string jsonString)
        {
            var moviesDto = JsonConvert.DeserializeObject<ImportMoviesDto[]>(jsonString);

            var sb = new StringBuilder();

            var movies = new List<Movie>();

            foreach (var dto in moviesDto)
            {
                if (!IsValid(dto))
                {
                    sb.AppendLine(ErrorMessage);
                    continue;
                }

                var movie = new Movie
                {
                    Title = dto.Title,
                    Genre = dto.Genre,
                    Duration = dto.Duration,
                    Rating = dto.Rating,
                    Director = dto.Director
                };

                movies.Add(movie);
                sb.AppendLine(string.Format(SuccessfulImportMovie, dto.Title, dto.Genre, dto.Rating.ToString("F2")));
            }

            context.Movies.AddRange(movies);
            context.SaveChanges();

            return sb.ToString().TrimEnd();
        }


        public static string ImportHallSeats(CinemaContext context, string jsonString)
        {
            var hallsDto = JsonConvert.DeserializeObject<ImportHallDto[]>(jsonString);

            var sb = new StringBuilder();

            foreach (var dto in hallsDto)
            {
                if (!IsValid(dto))
                {
                    sb.AppendLine(ErrorMessage);
                    continue;
                }

                var hall = new Hall
                {
                    Name = dto.Name,
                    Is4Dx = dto.Is4Dx,
                    Is3D = dto.Is3D
                };
                context.Halls.Add(hall);

                AddSeats(context, hall.Id, dto.Seats);

                string projectionType = TypeProjection(dto.Is3D, dto.Is4Dx);

                sb.AppendLine(string.Format(SuccessfulImportHallSeat, dto.Name, projectionType, dto.Seats));

            }

            context.SaveChanges();
            return sb.ToString().TrimEnd();
        }


        public static string ImportProjections(CinemaContext context, string xmlString)
        {
            var xmlSerializer = new XmlSerializer(typeof(ImportProjectionDto[]),
                           new XmlRootAttribute("Projections"));

            var objects = (ImportProjectionDto[])xmlSerializer
                .Deserialize(new StringReader(xmlString));

            var sb = new StringBuilder();
            
            foreach (var dto in objects)
            {
                bool isValidMovie = context.Movies.Any(m => m.Id == dto.MovieId);
                bool isValidHall = context.Halls.Any(h => h.Id == dto.HallId);

                if (!IsValid(dto) || !isValidMovie || !isValidHall)
                {
                    sb.AppendLine(ErrorMessage);
                    continue;
                }

                var projection = new Projection
                {
                    MovieId = dto.MovieId,
                    HallId = dto.HallId,
                    DateTime = DateTime.ParseExact(
                        dto.DateTime, @"yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
                };

                context.Projections.Add(projection);
                var dateTimeRes = projection.DateTime.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture);
                sb.AppendLine(string.Format(SuccessfulImportProjection, projection.Movie.Title, dateTimeRes));
            }

            context.SaveChanges();

            var result = sb.ToString().TrimEnd();

            return result;
        }

        public static string ImportCustomerTickets(CinemaContext context, string xmlString)
        {
            var xmlSerializer = new XmlSerializer(typeof(ImportCutomersTicketDto[]),
                           new XmlRootAttribute("Customers"));

            var objects = (ImportCutomersTicketDto[])xmlSerializer
                .Deserialize(new StringReader(xmlString));

            var sb = new StringBuilder();

            foreach (var dto in objects)
            {
                if (!IsValid(dto))
                {
                    sb.AppendLine(ErrorMessage);
                    continue;
                }

                var customer = new Customer
                {
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    Age = dto.Age,
                    Balance = dto.Balance
                };

                context.Customers.Add(customer);

                AddCustomerTickets(context, customer.Id, dto.Tickets);
                sb.AppendLine(string.Format(
                        SuccessfulImportCustomerTicket,
                        dto.FirstName,
                        dto.LastName,
                        dto.Tickets.Count()));
            }
                                                  
            context.SaveChanges();

            var result = sb.ToString().TrimEnd();

            return result;
        }

        private static void AddCustomerTickets(CinemaContext context, int customerId, TicketDto[] ticketsDto)
        {
            var tickets = new List<Ticket>();

            foreach (var dto in ticketsDto)
            {
                if (IsValid(dto))
                {
                    var ticket = new Ticket
                    {
                        ProjectionId = dto.ProjectionId,
                        CustomerId = customerId,
                        Price = dto.Price
                    };
                    tickets.Add(ticket);
                }
            }

            context.Tickets.AddRange(tickets);
            context.SaveChanges();
        }

        private static string TypeProjection(bool is3D, bool is4Dx)
        {
            var result = "Normal";
            if (is3D == true && is4Dx == true)
            {
                result = "4Dx/3D";
            }
            else if (is3D == true && is4Dx == false)
            {
                result = "3D";
            }
            else if (is3D == false && is4Dx == true)
            {
                result = "4Dx";
            }

            return result;
        }

        private static void AddSeats(CinemaContext context, int hallId, int countSeats)
        {
            var seats = new List<Seat>();

            for (int i = 0; i < countSeats; i++)
            {
                seats.Add(new Seat { HallId = hallId });
            }
            context.AddRange(seats);
            context.SaveChanges();
        }


        private static bool IsValid(this object obj)
        {
            var validationContext = new ValidationContext(obj);
            var validationResults = new List<ValidationResult>();

            var isValid = Validator.TryValidateObject(obj, validationContext, validationResults, true);

            return isValid;
        }

    }
}