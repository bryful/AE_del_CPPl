using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace AE_del_CPPl
{
	public partial class AE_CPPl : Component
	{

		private TextBox? m_TextBox = null;
		public TextBox? TextBox
		{
			get { return m_TextBox; }
			set
			{ 
				m_TextBox = value; 
				if(m_TextBox!=null)
				{
					m_TextBox.Text = "";
				}
			}
		}
		private int adr_RIFX = 0;
		private uint value_RIFX = 0;
		private int adr_LIST = 0;
		private uint value_LIST = 0;
		private int adr_CPPl = 0;
		private int adr_cpid = 0;
		private int length_CPPl_cpid = 0;

		// ***************************************************************************
		private byte[] m_aep = new byte[0];
		// ***************************************************************************
		private string RIFX = "RIFX";
		// ***************************************************************************
		private string m_FileName = "";
		public string FileName
		{
			get { return m_FileName; }
			set
			{
				m_FileName = value;
			}
		}
		// ***************************************************************************
		public string Info
		{
			get
			{
				string ret = "";

				ret += String.Format("RIFX:{0}byte\r\n", value_RIFX);
				ret += String.Format("LIST:{0}byte\r\n", value_LIST);
				ret += String.Format("CPPl:{0}\r\n", adr_CPPl);
				ret += String.Format("cpid:{0}\r\n", adr_cpid);
				ret += String.Format("    :{0}byte\r\n", length_CPPl_cpid);
				ret += String.Format("{0}kbyteのCPPlがあります\r\n", length_CPPl_cpid/1024);

				return ret;
			}
		}
		public bool DeleteCPPl()
		{
			bool ret = false;
			int filesize = m_aep.Length;
			int delSize = (int)(value_LIST - 4);

			int nfilesize = m_aep.Length -delSize ;


			uint nValue_RIFX = value_RIFX - (uint)delSize;
			uint nValue_LIST = 4;


			for (int i=adr_cpid;i< filesize;i++)
			{
				m_aep[i - delSize] = m_aep[i];
			}

			WriteUint(adr_RIFX+4, nValue_RIFX);
			WriteUint(adr_LIST+4, nValue_LIST);
			Array.Resize(ref m_aep, nfilesize);
			ret = true;
			return ret;
		}
		// ********************************************************************************************
		public void Init()
		{
			adr_RIFX = 0;
			value_RIFX = 0;
			adr_LIST = 0;
			value_LIST = 0;
			adr_CPPl = 0;
			adr_cpid = 0;
			length_CPPl_cpid = 0;
		}
	// ********************************************************************************************
	private bool Analysis()
		{
			Init();
			bool ret = false;
			adr_RIFX = Find("RIFX", 0);
			if (adr_RIFX != 0) return ret;
			value_RIFX = ReadUint(adr_RIFX + 4);
			adr_CPPl = Find("CPPl", 8);
			if (adr_CPPl <= 0) return ret;
			adr_LIST = Find("LIST", adr_CPPl - 32);
			if (adr_LIST < 0) return ret;
			value_LIST = ReadUint(adr_LIST + 4);
			adr_cpid = Find("cpid", adr_CPPl + 4);
			if (adr_cpid < 0) return ret;
			length_CPPl_cpid = adr_cpid - adr_CPPl;
			return true;
		}
		// ********************************************************************************************
		private int Find(char[] a, int idx = 0)
		{
			byte[] b = new byte[a.Length];
			for (int i = 0; i < a.Length; i++) b[i] = (byte)a[i];
			return Find(b, idx);
		}
		// ********************************************************************************************
		private int Find(string a, int idx = 0)
		{
			if (a.Length == 0) return -1;
			byte[] byteArray = Encoding.ASCII.GetBytes(a);
			return Find(byteArray, idx);
		}
		// ********************************************************************************************
		private int Find(byte[] a, int idx = 0)
		{
			int ret = -1;
			if (a.Length <= 0) return ret;
			int cnt = m_aep.Length - a.Length;
			if (idx >= cnt) return ret;
			for (int i = idx; i < cnt; i++)
			{
				if (m_aep[i] == a[0])
				{
					if (a.Length == 1)
					{
						ret = i;
						break;
					}
					else
					{
						bool b = true;
						for (int j = 1; j < a.Length; j++)
						{
							if (m_aep[i + j] != a[j])
							{
								b = false;
								break;
							}
						}
						if (b == true)
						{
							ret = i;
							break;
						}
					}
				}
			}
			return ret;
		}
		// ********************************************************************************************
		public uint FindTagSize(string tag, int idx)
		{
			uint ret = 0;
			int i = Find(tag, idx);
			if (i < 0) return ret;
			i += tag.Length;
			if (i >= m_aep.Length - 4) return ret;
			ret = ReadUint(i);
			return ret;
		}
		public uint ReadUint(int idx)
		{
			uint ret = 0;

			if (idx < m_aep.Length - 4)
			{
				ret = (uint)m_aep[idx] << 24 | ((uint)m_aep[idx + 1] << 16) | ((uint)m_aep[idx + 2] << 8) | ((uint)m_aep[idx + 3]);
			}


			return ret;
		}
		public void WriteUint(int idx, uint v)
		{
			if (idx < m_aep.Length - 4)
			{
				m_aep[idx + 0] = (byte)((v >> 24) & 0xFF);
				m_aep[idx + 1] = (byte)((v >> 16) & 0xFF);
				m_aep[idx + 2] = (byte)((v >> 08) & 0xFF);
				m_aep[idx + 3] = (byte)((v) & 0xFF);
			}
		}       // ***************************************************************************
		public AE_CPPl()
		{
			InitializeComponent();
		}

		public AE_CPPl(IContainer container)
		{
			container.Add(this);

			InitializeComponent();
		}
		// ********************************************************************************************
		public bool Export()
		{
			bool ret = false;
			if (m_aep.Length <= 0) return ret;
			if (m_FileName == "") return ret;
			string nfilename = Path.GetFileNameWithoutExtension(m_FileName) + "_deleted.aep";
			nfilename = Path.Combine(Path.GetDirectoryName(m_FileName), nfilename);
			Analysis();
			var b= DeleteCPPl();
			if(b==false)
			{
				if(m_TextBox!=null)
				{
					m_TextBox.Text = "失敗";
				}
				return ret;
			}
			FileStream fs = new FileStream(
				nfilename,
				System.IO.FileMode.Create,
				System.IO.FileAccess.Write);
			try
			{
				fs.Write(m_aep, 0, m_aep.Length);
				ret = true;
				Analysis();
				if(m_TextBox!=null)
				{
					m_TextBox.Text = Info;
				}
			}
			finally
			{
				fs.Close();
			}
			return ret;

		}
		// ********************************************************************************************
		public bool AepLoad(string p)
		{
			bool ret = false;
			FileStream fs = new FileStream(
				p,
				System.IO.FileMode.Open,
				System.IO.FileAccess.Read);
			try
			{
				m_aep = new byte[fs.Length];
				int rd = fs.Read(m_aep, 0, m_aep.Length);
				ret = (rd == m_aep.Length);
				if(ret)
				{
					ret = (Find(RIFX, 0) == 0);
					if(ret)
					{
						m_FileName = p;
						Analysis();
						if (m_TextBox!=null)
						{
							m_TextBox.Text = Info;
						}
					}
				}
			}
			finally
			{
				fs.Close();
			}
			return ret;
		}

		// ********************************************************************************************
		public string Test()
		{
			string ret = "";
			int CPPl = Find("CPPl", 0);
			if (CPPl < 0)
			{
				ret = "no CPPl";
				return ret;
			}
			int LIST = Find("LIST", CPPl-32);
			uint LiSTV = ReadUint(LIST + 4);

			int cpid = Find("cpid", CPPl);
			if (CPPl < 0)
			{
				ret = "no cpid";
				return ret;
			}

			ret = string.Format("LIST:{0}\r\nCPPl:{1}\r\ncpid:{2}\r\n    :{3}",
				LiSTV,
				CPPl,
				cpid,
				cpid-CPPl
				);

			return ret;
		}
		// ********************************************************************************************
	}
}
