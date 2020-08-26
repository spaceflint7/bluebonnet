
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    // QueryHighScores creates anonymous classes that we need to export.
    // the FilterAttribute is queried in BaseTest::TestSecondary()
    [FilterAttribute("*Anonymous*")]
    public class TestLinq : BaseTest
    {

        public override void TestMain()
        {
            Test1();
            Test2();
            Test3();
            TestEnum();
        }

        void Test1()
        {
            int[] scores = new int[] { 97, 92, 81, 60 };

            IEnumerable<int> scoreQuery =
                from score in scores
                where score > 80
                select score;

            foreach (int i in scoreQuery)
            {
                Console.Write(i + " ");
            }
        }

        //
        // Test2
        //

        void Test2()
        {
            Student.QueryHighScores(1, 90);
            Student.QueryByIds();
        }

        public class Student
        {
            #region data
            public enum GradeLevel { FirstYear = 1, SecondYear, ThirdYear, FourthYear };

            public string FirstName { get; set; }
            public string LastName { get; set; }
            public int Id { get; set; }
            public GradeLevel Year;
            public List<int> ExamScores;

            protected static List<Student> students = new List<Student>
            {
                new Student {FirstName = "Terry", LastName = "Adams", Id = 120,
                    Year = GradeLevel.SecondYear,
                    ExamScores = new List<int> { 99, 82, 81, 79}},
                new Student {FirstName = "Fadi", LastName = "Fakhouri", Id = 116,
                    Year = GradeLevel.ThirdYear,
                    ExamScores = new List<int> { 99, 86, 90, 94}},
                new Student {FirstName = "Hanying", LastName = "Feng", Id = 117,
                    Year = GradeLevel.FirstYear,
                    ExamScores = new List<int> { 93, 92, 80, 87}},
                new Student {FirstName = "Cesar", LastName = "Garcia", Id = 114,
                    Year = GradeLevel.FourthYear,
                    ExamScores = new List<int> { 97, 89, 85, 82}},
                new Student {FirstName = "Debra", LastName = "Garcia", Id = 115,
                    Year = GradeLevel.ThirdYear,
                    ExamScores = new List<int> { 35, 72, 91, 70}},
                new Student {FirstName = "Hugo", LastName = "Garcia", Id = 118,
                    Year = GradeLevel.SecondYear,
                    ExamScores = new List<int> { 92, 90, 83, 78}},
                new Student {FirstName = "Sven", LastName = "Mortensen", Id = 113,
                    Year = GradeLevel.FirstYear,
                    ExamScores = new List<int> { 88, 94, 65, 91}},
                new Student {FirstName = "Claire", LastName = "O'Donnell", Id = 112,
                    Year = GradeLevel.FourthYear,
                    ExamScores = new List<int> { 75, 84, 91, 39}},
                new Student {FirstName = "Svetlana", LastName = "Omelchenko", Id = 111,
                    Year = GradeLevel.SecondYear,
                    ExamScores = new List<int> { 97, 92, 81, 60}},
                new Student {FirstName = "Lance", LastName = "Tucker", Id = 119,
                    Year = GradeLevel.ThirdYear,
                    ExamScores = new List<int> { 68, 79, 88, 92}},
                new Student {FirstName = "Michael", LastName = "Tucker", Id = 122,
                    Year = GradeLevel.FirstYear,
                    ExamScores = new List<int> { 94, 92, 91, 91}},
                new Student {FirstName = "Eugene", LastName = "Zabokritski", Id = 121,
                    Year = GradeLevel.FourthYear,
                    ExamScores = new List<int> { 96, 85, 91, 60}}
            };
            #endregion

            // Helper method, used in GroupByRange.
            protected static int GetPercentile(Student s)
            {
                double avg = s.ExamScores.Average();
                return avg > 0 ? (int)avg / 10 : 0;
            }

            public static void QueryHighScores(int exam, int score)
            {
                var highScores = from student in students
                                 where student.ExamScores[exam] > score
                                 select new {Name = student.FirstName, Score = student.ExamScores[exam]};

                foreach (var item in highScores)
                {
                    Console.WriteLine($"{item.Name,-15}{item.Score}");
                }
            }

            public static void QueryByIds()
            {
                string[] ids = { "111", "114", "112" };

                var queryNames =
                    from student in students
                    let i = student.Id.ToString()
                    where ids.Contains(i)
                    select new { student.LastName, student.Id };

                foreach (var name in queryNames)
                {
                    Console.WriteLine($"{name.LastName}: {name.Id}");
                }
            }
        }

        //
        //
        //

        public static void Test3()
        {
            var source = Enumerable.Range(100, 20000);

            // Result sequence might be out of order.
            var parallelQuery = from num in source.AsParallel()
                                where num % 10 == 0
                                select num;

            // Process result sequence in parallel
            int sum = 0;
            parallelQuery.ForAll((e) => DoSomething(e));
            Console.WriteLine("Parallel Linq Sum " + sum);

            // Or use foreach to merge results first.
            sum = 0;
            foreach (var n in parallelQuery) sum += n;
            Console.WriteLine("Parallel Linq Sum " + sum);

            // You can also use ToArray, ToList, etc as with LINQ to Objects.
            var parallelQuery2 = (from num in source.AsParallel()
                                  where num % 10 == 0
                                  select num).ToArray();

            // Method syntax is also supported
            var parallelQuery3 = source.AsParallel().Where(n => n % 10 == 0).Select(n => n);

            void DoSomething(int i) { System.Threading.Interlocked.Add(ref sum, i); }
        }

        //
        // TestEnum
        //

        void TestEnum()
        {
            var x = Test2();
            Console.WriteLine(x);

            System.Linq.ParallelExecutionMode? Test2()
            {
                m_executionMode = System.Linq.ParallelExecutionMode.Default;
                return m_executionMode;
            }
        }

        private System.Linq.ParallelExecutionMode? m_executionMode;
   }
}