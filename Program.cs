using Timer = System.Timers.Timer;

namespace LongPolling
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var app = builder.Build();

            app.MapGet("/submit", () => 
            {
                Job newJob = new Job()
                {
                    id = JobsData.lastestId,
                    status = 0
                };
                JobsData.lastestId++;
                JobsData.jobsList.Add(newJob);
                JobsData.StartUpdating(newJob.id);
                return Results.Ok($"Job {newJob.id} started ....");
            });

            app.MapGet("/status/{id:int}", (int id) =>
            {
                Job? job = JobsData.jobsList.FirstOrDefault(x => x.id == id);
                if (job == null)
                {
                    return Results.BadRequest($"No Job found with Id = {id}");
                }
                while (job.status < 100) ;                    
                return Results.Ok(job);
            });

            app.MapGet("/all-jobs", () => Results.Ok(JobsData.jobsList));

            app.Run();
        }
    }

    public class Job
    {
        public int id { get; set; }
        public int status { get; set; } 
    }

    public static class JobsData
    {
        public static int lastestId = 1;
        public static List<Job> jobsList = new List<Job>();

        public static Dictionary<int, Timer> ActiveTimers = new Dictionary<int, Timer>();

        public static void StartUpdating(int id)
        {
            if (ActiveTimers.ContainsKey(id)) return;

            Timer t = new Timer();
            t.Interval = 2000;
            t.AutoReset = true;

            t.Elapsed += (sender, e) =>
            {
                var job = jobsList.First(x => x.id == id);
                job.status += 10;
                if (job.status >= 100)
                {
                    t.Stop();
                    t.Dispose();
                    ActiveTimers.Remove(job.id);
                }
            };

            t.Start();
            ActiveTimers.Add(id, t);

        }
    }
}