namespace TeisterMask.DataProcessor
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    using ValidationContext = System.ComponentModel.DataAnnotations.ValidationContext;

    using Data;
    using System.Xml.Serialization;
    using TeisterMask.DataProcessor.ImportDto;
    using System.IO;
    using System.Text;
    using System.Linq;
    using System.Globalization;
    using TeisterMask.Data.Models;
    using Newtonsoft.Json;
    using TeisterMask.Data.Models.Enums;

    public class Deserializer
    {
        private const string ErrorMessage = "Invalid data!";

        private const string SuccessfullyImportedProject
            = "Successfully imported project - {0} with {1} tasks.";

        private const string SuccessfullyImportedEmployee
            = "Successfully imported employee - {0} with {1} tasks.";

        public static string ImportProjects(TeisterMaskContext context, string xmlString)
        {
            var xmlSerializer = new XmlSerializer(typeof(ImportProjectDto[]),
                           new XmlRootAttribute("Projects"));

            var objects = (ImportProjectDto[])xmlSerializer
                .Deserialize(new StringReader(xmlString));

            var sb = new StringBuilder();

            var projects = new List<Project>();

            foreach (var dto in objects)
            {
                if (!IsValid(dto))
                {
                    sb.AppendLine(ErrorMessage);
                    continue;
                }
                                
                var project = new Project
                {
                    Name = dto.Name,
                    OpenDate = DateTime.ParseExact(dto.OpenDate, "dd/MM/yyyy",
                             CultureInfo.InvariantCulture),
                    DueDate = string.IsNullOrEmpty(dto.DueDate) ?
                        (DateTime?)null
                        : DateTime.ParseExact(
                        dto.DueDate, @"dd/MM/yyyy", CultureInfo.InvariantCulture)
                };
                
                var tasks = new List<Task>();

                foreach (var taskDto in dto.Tasks)
                {
                    var taskOpenDate = DateTime.ParseExact(taskDto.OpenDate, "dd/MM/yyyy",
                                 CultureInfo.InvariantCulture);
                    var taskDueDate = DateTime.ParseExact(taskDto.DueDate, "dd/MM/yyyy",
                                 CultureInfo.InvariantCulture);
                    
                    // bool isValidExecutionType = Enum.IsDefined(typeof(ExecutionType), taskDto.ExecutionType);/
                    if (!IsValid(taskDto))
                    {
                        sb.AppendLine(ErrorMessage);
                        continue;
                    }

                    if (taskOpenDate < project.OpenDate || taskDueDate > project.DueDate)
                    {
                        sb.AppendLine(ErrorMessage);
                        continue;
                    }
                    var task = new Task
                    {
                        Name = taskDto.Name,
                        OpenDate = taskOpenDate,
                        DueDate = taskDueDate,
                        ExecutionType = (ExecutionType)
                            Enum.ToObject(typeof(ExecutionType), taskDto.ExecutionType),
                        LabelType = (LabelType)Enum.ToObject(typeof(ExecutionType), taskDto.LabelType),
                        Project = project
                    };
                    tasks.Add(task);
                    project.Tasks.Add(task);

                }

                projects.Add(project);

                sb.AppendLine(string.Format(SuccessfullyImportedProject, dto.Name, tasks.Count));

            }

            context.Projects.AddRange(projects);
            context.SaveChanges();

            var result = sb.ToString().TrimEnd();

            return result;

        }

        public static string ImportEmployees(TeisterMaskContext context, string jsonString)
        {
            var employeesDto = JsonConvert.DeserializeObject<ImportEmployeeDto[]>(jsonString);

            var sb = new StringBuilder();

            var employees = new List<Employee>();

            foreach (var dto in employeesDto)
            {
                if (!IsValid(dto))
                {
                    sb.AppendLine(ErrorMessage);
                    continue;
                }

                var emplo = new Employee
                {
                    Username = dto.Username,
                    Email = dto.Email,
                    Phone = dto.Phone
                };

                foreach (var taskId in dto.Tasks.Distinct())
                {
                    if (context.Tasks.Find(taskId) == null)
                    {
                        sb.AppendLine(ErrorMessage);
                        continue;
                    }

                    emplo.EmployeesTasks
                        .Add(new EmployeeTask { TaskId = taskId });
                }
                employees.Add(emplo);
                sb.AppendLine(string.Format(SuccessfullyImportedEmployee,
                    emplo.Username,
                    emplo.EmployeesTasks.Count));

            }

            context.Employees.AddRange(employees);
            context.SaveChanges();

            var result = sb.ToString().TrimEnd();

            return result;
        }

        private static bool IsValid(object dto)
        {
            var validationContext = new ValidationContext(dto);
            var validationResult = new List<ValidationResult>();

            return Validator.TryValidateObject(dto, validationContext, validationResult, true);
        }
    }
}