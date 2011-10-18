using System;
using System.Collections.Generic;
using System.Text;

namespace BlueBlocksLib.Database
{


	public class DBConnection
    {
		IDatabaseImplementation m_idbImpl;

		public DBConnection(IDatabaseImplementation impl) {
			m_idbImpl = impl;
		}

        public void Insert<T>(string table, T values)
        {

        }

    }


}
