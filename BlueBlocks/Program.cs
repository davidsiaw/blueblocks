using System;
using System.Collections.Generic;
using System.Text;
using BlueBlocksLib.Database;

namespace BlueBlocks
{
    class Program
    {
		[Column]
		interface EnrollmentStudent : ForeignKey<Student> { Student Student { get; set; } }

		[Column]
		interface EnrollmentClass : ForeignKey<Class> { Class Class { get; set; } }

		[Table]
		interface Enrollment : EnrollmentClass, EnrollmentStudent { }


		[Column]
		interface Year { int Year { get; set; } }

		[Column]
		interface Level { string Level { get; set; } }

		[Table]
		interface Class : Year, Level { }


		[Column]
		interface Name { string Name { get; set; } }

		[Column]
		interface Age { string Age { get; set; } };

		[Table]
		interface Student : Name, Age { }

		[Database]
		interface TestDB : Student, Class, Enrollment { }



        static void Main(string[] args)
        {

			DBConnection<TestDB> testdb = new DBConnection<TestDB>(null);
        }
    }
}
