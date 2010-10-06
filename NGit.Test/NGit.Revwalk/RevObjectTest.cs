using NGit;
using NGit.Revwalk;
using Sharpen;

namespace NGit.Revwalk
{
	public class RevObjectTest : RevWalkTestCase
	{
		/// <exception cref="System.Exception"></exception>
		public virtual void TestId()
		{
			RevCommit a = Commit();
			NUnit.Framework.Assert.AreSame(a, a.Id);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestEquals()
		{
			RevCommit a1 = Commit();
			RevCommit b1 = Commit();
			NUnit.Framework.Assert.IsTrue(a1.Equals(a1));
			NUnit.Framework.Assert.IsTrue(a1.Equals((object)a1));
			NUnit.Framework.Assert.IsFalse(a1.Equals(b1));
			NUnit.Framework.Assert.IsTrue(a1.Equals(a1));
			NUnit.Framework.Assert.IsTrue(a1.Equals((object)a1));
			NUnit.Framework.Assert.IsFalse(a1.Equals(string.Empty));
			RevWalk rw2 = new RevWalk(db);
			RevCommit a2 = rw2.ParseCommit(a1);
			RevCommit b2 = rw2.ParseCommit(b1);
			NUnit.Framework.Assert.AreNotSame(a1, a2);
			NUnit.Framework.Assert.AreNotSame(b1, b2);
			NUnit.Framework.Assert.IsTrue(a1.Equals(a2));
			NUnit.Framework.Assert.IsTrue(b1.Equals(b2));
			NUnit.Framework.Assert.AreEqual(a1.GetHashCode(), a2.GetHashCode());
			NUnit.Framework.Assert.AreEqual(b1.GetHashCode(), b2.GetHashCode());
			NUnit.Framework.Assert.IsTrue(AnyObjectId.Equals(a1, a2));
			NUnit.Framework.Assert.IsTrue(AnyObjectId.Equals(b1, b2));
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestRevObjectTypes()
		{
			NUnit.Framework.Assert.AreEqual(Constants.OBJ_TREE, Tree().Type);
			NUnit.Framework.Assert.AreEqual(Constants.OBJ_COMMIT, Commit().Type);
			NUnit.Framework.Assert.AreEqual(Constants.OBJ_BLOB, Blob(string.Empty).Type);
			NUnit.Framework.Assert.AreEqual(Constants.OBJ_TAG, Tag("emptyTree", Tree()).Type);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestHasRevFlag()
		{
			RevCommit a = Commit();
			NUnit.Framework.Assert.IsFalse(a.Has(RevFlag.UNINTERESTING));
			a.flags |= RevWalk.UNINTERESTING;
			NUnit.Framework.Assert.IsTrue(a.Has(RevFlag.UNINTERESTING));
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestHasAnyFlag()
		{
			RevCommit a = Commit();
			RevFlag flag1 = rw.NewFlag("flag1");
			RevFlag flag2 = rw.NewFlag("flag2");
			RevFlagSet s = new RevFlagSet();
			s.AddItem(flag1);
			s.AddItem(flag2);
			NUnit.Framework.Assert.IsFalse(a.HasAny(s));
			a.flags |= flag1.mask;
			NUnit.Framework.Assert.IsTrue(a.HasAny(s));
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestHasAllFlag()
		{
			RevCommit a = Commit();
			RevFlag flag1 = rw.NewFlag("flag1");
			RevFlag flag2 = rw.NewFlag("flag2");
			RevFlagSet s = new RevFlagSet();
			s.AddItem(flag1);
			s.AddItem(flag2);
			NUnit.Framework.Assert.IsFalse(a.HasAll(s));
			a.flags |= flag1.mask;
			NUnit.Framework.Assert.IsFalse(a.HasAll(s));
			a.flags |= flag2.mask;
			NUnit.Framework.Assert.IsTrue(a.HasAll(s));
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestAddRevFlag()
		{
			RevCommit a = Commit();
			RevFlag flag1 = rw.NewFlag("flag1");
			RevFlag flag2 = rw.NewFlag("flag2");
			NUnit.Framework.Assert.AreEqual(0, a.flags);
			a.Add(flag1);
			NUnit.Framework.Assert.AreEqual(flag1.mask, a.flags);
			a.Add(flag2);
			NUnit.Framework.Assert.AreEqual(flag1.mask | flag2.mask, a.flags);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestAddRevFlagSet()
		{
			RevCommit a = Commit();
			RevFlag flag1 = rw.NewFlag("flag1");
			RevFlag flag2 = rw.NewFlag("flag2");
			RevFlagSet s = new RevFlagSet();
			s.AddItem(flag1);
			s.AddItem(flag2);
			NUnit.Framework.Assert.AreEqual(0, a.flags);
			a.Add(s);
			NUnit.Framework.Assert.AreEqual(flag1.mask | flag2.mask, a.flags);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestRemoveRevFlag()
		{
			RevCommit a = Commit();
			RevFlag flag1 = rw.NewFlag("flag1");
			RevFlag flag2 = rw.NewFlag("flag2");
			a.Add(flag1);
			a.Add(flag2);
			NUnit.Framework.Assert.AreEqual(flag1.mask | flag2.mask, a.flags);
			a.Remove(flag2);
			NUnit.Framework.Assert.AreEqual(flag1.mask, a.flags);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestRemoveRevFlagSet()
		{
			RevCommit a = Commit();
			RevFlag flag1 = rw.NewFlag("flag1");
			RevFlag flag2 = rw.NewFlag("flag2");
			RevFlag flag3 = rw.NewFlag("flag3");
			RevFlagSet s = new RevFlagSet();
			s.AddItem(flag1);
			s.AddItem(flag2);
			a.Add(flag3);
			a.Add(s);
			NUnit.Framework.Assert.AreEqual(flag1.mask | flag2.mask | flag3.mask, a.flags);
			a.Remove(s);
			NUnit.Framework.Assert.AreEqual(flag3.mask, a.flags);
		}
	}
}