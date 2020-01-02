namespace Cinema.DataProcessor
{
    using System;
    using System.Linq;
    using System.Text;
    using System.Xml.Serialization;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    using Data;
    using System.IO;
    using Newtonsoft.Json;
    using Cinema.Data.Models;
    using System.Globalization;
    using Cinema.DataProcessor.ImportDto;

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
            var moviesDtos = JsonConvert.DeserializeObject<ImportMovieDto[]>(jsonString);

            var movies = new List<Movie>();

            var sb = new StringBuilder();


            foreach (var dto in moviesDtos)
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
            var hallSeatsDto = JsonConvert.DeserializeObject<HallSeatImportDto[]>(jsonString);

            var sb = new StringBuilder();

            foreach (var dto in hallSeatsDto)
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


                AddSeatsInHall(context, hall.Id, dto.Seats);

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
                if (IsValid(dto) && IsValidMovieId(context, dto.MovieId) && IsvalidHallId(context, dto.HallId))
                {
                    var projection = new Projection
                    {
                        MovieId = dto.MovieId,
                        HallId = dto.HallId,
                        DateTime = DateTime.ParseExact(
                            dto.DateTime,
                            "yyyy-MM-dd HH:mm:ss",
                            CultureInfo.InvariantCulture
                            )
                    };

                    context.Projections.Add(projection);
                    var dateTimeRes = projection.DateTime.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture);
                    sb.AppendLine(string.Format(SuccessfulImportProjection, projection.Movie.Title, dateTimeRes));
                }
                else
                {
                    sb.AppendLine(ErrorMessage);
                }
            }

            context.SaveChanges();

            return sb.ToString().TrimEnd();
        }

        public static string ImportCustomerTickets(CinemaContext context, string xmlString)
        {
            var xmlSerializer = new XmlSerializer(typeof(ImportCustomerTicketDto[]),
               new XmlRootAttribute("Customers"));

            var objects = (ImportCustomerTicketDto[])xmlSerializer
                .Deserialize(new StringReader(xmlString));

            var sb = new StringBuilder();
            foreach (var dto in objects)
            {
                if (IsValid(dto))
                {
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
                else
                {
                    sb.AppendLine(ErrorMessage);
                }
            }

            context.SaveChanges();

            return sb.ToString().TrimEnd();
        }

        private static void AddCustomerTickets(CinemaContext context, 
                    int customerId, TicketsCustomerImportDto[] dtoTickets)
        {
            var tickets = new List<Ticket>();

            foreach (var dto in dtoTickets)
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


        private static bool IsvalidHallId(CinemaContext context, int hallId)
        {
            return context.Halls.Any(h => h.Id == hallId);
        }


        private static bool IsValidMovieId(CinemaContext context, int movieId)
        {
            return context.Movies.Any(m => m.Id == movieId);
        }


        private static void AddSeatsInHall(CinemaContext context, int hallId, int countSeats)
        {
            var seats = new List<Seat>();

            for (int i = 0; i < countSeats; i++)
            {
                seats.Add(new Seat { HallId = hallId });
            }
            context.AddRange(seats);
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


        private static bool IsValid(this object obj)
        {
            var validationContext = new ValidationContext(obj);
            var validationResults = new List<ValidationResult>();

            var isValid = Validator.TryValidateObject(obj, validationContext, validationResults, true);

            return isValid;
        }
    }
}