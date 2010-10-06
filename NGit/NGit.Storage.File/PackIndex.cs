using System;
using System.Collections.Generic;
using System.IO;
using NGit;
using NGit.Storage.File;
using NGit.Util;
using Sharpen;

namespace NGit.Storage.File
{
	/// <summary>
	/// Access path to locate objects by
	/// <see cref="NGit.ObjectId">NGit.ObjectId</see>
	/// in a
	/// <see cref="PackFile">PackFile</see>
	/// .
	/// <p>
	/// Indexes are strictly redundant information in that we can rebuild all of the
	/// data held in the index file from the on disk representation of the pack file
	/// itself, but it is faster to access for random requests because data is stored
	/// by ObjectId.
	/// </p>
	/// </summary>
	public abstract class PackIndex : Iterable<PackIndex.MutableEntry>
	{
		/// <summary>Open an existing pack <code>.idx</code> file for reading.</summary>
		/// <remarks>
		/// Open an existing pack <code>.idx</code> file for reading.
		/// <p>
		/// The format of the file will be automatically detected and a proper access
		/// implementation for that format will be constructed and returned to the
		/// caller. The file may or may not be held open by the returned instance.
		/// </p>
		/// </remarks>
		/// <param name="idxFile">existing pack .idx to read.</param>
		/// <returns>access implementation for the requested file.</returns>
		/// <exception cref="System.IO.FileNotFoundException">the file does not exist.</exception>
		/// <exception cref="System.IO.IOException">
		/// the file exists but could not be read due to security errors,
		/// unrecognized data version, or unexpected data corruption.
		/// </exception>
		public static PackIndex Open(FilePath idxFile)
		{
			FileInputStream fd = new FileInputStream(idxFile);
			try
			{
				byte[] hdr = new byte[8];
				IOUtil.ReadFully(fd, hdr, 0, hdr.Length);
				if (IsTOC(hdr))
				{
					int v = NB.DecodeInt32(hdr, 4);
					switch (v)
					{
						case 2:
						{
							return new PackIndexV2(fd);
						}

						default:
						{
							throw new IOException(MessageFormat.Format(JGitText.Get().unsupportedPackIndexVersion
								, v));
						}
					}
				}
				return new PackIndexV1(fd, hdr);
			}
			catch (IOException ioe)
			{
				string path = idxFile.GetAbsolutePath();
				IOException err;
				err = new IOException(MessageFormat.Format(JGitText.Get().unreadablePackIndex, path
					));
				Sharpen.Extensions.InitCause(err, ioe);
				throw err;
			}
			finally
			{
				try
				{
					fd.Close();
				}
				catch (IOException)
				{
				}
			}
		}

		// ignore
		private static bool IsTOC(byte[] h)
		{
			byte[] toc = PackIndexWriter.TOC;
			for (int i = 0; i < toc.Length; i++)
			{
				if (h[i] != toc[i])
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>Footer checksum applied on the bottom of the pack file.</summary>
		/// <remarks>Footer checksum applied on the bottom of the pack file.</remarks>
		protected internal byte[] packChecksum;

		/// <summary>Determine if an object is contained within the pack file.</summary>
		/// <remarks>Determine if an object is contained within the pack file.</remarks>
		/// <param name="id">the object to look for. Must not be null.</param>
		/// <returns>true if the object is listed in this index; false otherwise.</returns>
		public virtual bool HasObject(AnyObjectId id)
		{
			return FindOffset(id) != -1;
		}

		/// <summary>Provide iterator that gives access to index entries.</summary>
		/// <remarks>
		/// Provide iterator that gives access to index entries. Note, that iterator
		/// returns reference to mutable object, the same reference in each call -
		/// for performance reason. If client needs immutable objects, it must copy
		/// returned object on its own.
		/// <p>
		/// Iterator returns objects in SHA-1 lexicographical order.
		/// </p>
		/// </remarks>
		/// <returns>iterator over pack index entries</returns>
		public abstract override Sharpen.Iterator<PackIndex.MutableEntry> Iterator();

		/// <summary>Obtain the total number of objects described by this index.</summary>
		/// <remarks>Obtain the total number of objects described by this index.</remarks>
		/// <returns>
		/// number of objects in this index, and likewise in the associated
		/// pack that this index was generated from.
		/// </returns>
		internal abstract long GetObjectCount();

		/// <summary>Obtain the total number of objects needing 64 bit offsets.</summary>
		/// <remarks>Obtain the total number of objects needing 64 bit offsets.</remarks>
		/// <returns>
		/// number of objects in this index using a 64 bit offset; that is an
		/// object positioned after the 2 GB position within the file.
		/// </returns>
		internal abstract long GetOffset64Count();

		/// <summary>
		/// Get ObjectId for the n-th object entry returned by
		/// <see cref="Iterator()">Iterator()</see>
		/// .
		/// <p>
		/// This method is a constant-time replacement for the following loop:
		/// <pre>
		/// Iterator&lt;MutableEntry&gt; eItr = index.iterator();
		/// int curPosition = 0;
		/// while (eItr.hasNext() &amp;&amp; curPosition++ &lt; nthPosition)
		/// eItr.next();
		/// ObjectId result = eItr.next().toObjectId();
		/// </pre>
		/// </summary>
		/// <param name="nthPosition">
		/// position within the traversal of
		/// <see cref="Iterator()">Iterator()</see>
		/// that the
		/// caller needs the object for. The first returned
		/// <see cref="MutableEntry">MutableEntry</see>
		/// is 0, the second is 1, etc.
		/// </param>
		/// <returns>the ObjectId for the corresponding entry.</returns>
		internal abstract ObjectId GetObjectId(long nthPosition);

		/// <summary>
		/// Get ObjectId for the n-th object entry returned by
		/// <see cref="Iterator()">Iterator()</see>
		/// .
		/// <p>
		/// This method is a constant-time replacement for the following loop:
		/// <pre>
		/// Iterator&lt;MutableEntry&gt; eItr = index.iterator();
		/// int curPosition = 0;
		/// while (eItr.hasNext() &amp;&amp; curPosition++ &lt; nthPosition)
		/// eItr.next();
		/// ObjectId result = eItr.next().toObjectId();
		/// </pre>
		/// </summary>
		/// <param name="nthPosition">
		/// unsigned 32 bit position within the traversal of
		/// <see cref="Iterator()">Iterator()</see>
		/// that the caller needs the object for. The
		/// first returned
		/// <see cref="MutableEntry">MutableEntry</see>
		/// is 0, the second is 1,
		/// etc. Positions past 2**31-1 are negative, but still valid.
		/// </param>
		/// <returns>the ObjectId for the corresponding entry.</returns>
		internal ObjectId GetObjectId(int nthPosition)
		{
			if (nthPosition >= 0)
			{
				return GetObjectId((long)nthPosition);
			}
			int u31 = (int)(((uint)nthPosition) >> 1);
			int one = nthPosition & 1;
			return GetObjectId(((long)u31) << 1 | one);
		}

		/// <summary>Locate the file offset position for the requested object.</summary>
		/// <remarks>Locate the file offset position for the requested object.</remarks>
		/// <param name="objId">name of the object to locate within the pack.</param>
		/// <returns>
		/// offset of the object's header and compressed content; -1 if the
		/// object does not exist in this index and is thus not stored in the
		/// associated pack.
		/// </returns>
		internal abstract long FindOffset(AnyObjectId objId);

		/// <summary>
		/// Retrieve stored CRC32 checksum of the requested object raw-data
		/// (including header).
		/// </summary>
		/// <remarks>
		/// Retrieve stored CRC32 checksum of the requested object raw-data
		/// (including header).
		/// </remarks>
		/// <param name="objId">id of object to look for</param>
		/// <returns>CRC32 checksum of specified object (at 32 less significant bits)</returns>
		/// <exception cref="NGit.Errors.MissingObjectException">when requested ObjectId was not found in this index
		/// 	</exception>
		/// <exception cref="System.NotSupportedException">when this index doesn't support CRC32 checksum
		/// 	</exception>
		internal abstract long FindCRC32(AnyObjectId objId);

		/// <summary>Check whether this index supports (has) CRC32 checksums for objects.</summary>
		/// <remarks>Check whether this index supports (has) CRC32 checksums for objects.</remarks>
		/// <returns>true if CRC32 is stored, false otherwise</returns>
		internal abstract bool HasCRC32Support();

		/// <exception cref="System.IO.IOException"></exception>
		internal abstract void Resolve(ICollection<ObjectId> matches, AbbreviatedObjectId
			 id, int matchLimit);

		/// <summary>
		/// Represent mutable entry of pack index consisting of object id and offset
		/// in pack (both mutable).
		/// </summary>
		/// <remarks>
		/// Represent mutable entry of pack index consisting of object id and offset
		/// in pack (both mutable).
		/// </remarks>
		public class MutableEntry
		{
			internal readonly MutableObjectId idBuffer = new MutableObjectId();

			internal long offset;

			/// <summary>Returns offset for this index object entry</summary>
			/// <returns>offset of this object in a pack file</returns>
			public virtual long GetOffset()
			{
				return offset;
			}

			/// <returns>hex string describing the object id of this entry.</returns>
			public virtual string Name()
			{
				EnsureId();
				return idBuffer.Name;
			}

			/// <returns>a copy of the object id.</returns>
			public virtual ObjectId ToObjectId()
			{
				EnsureId();
				return idBuffer.ToObjectId();
			}

			/// <returns>a complete copy of this entry, that won't modify</returns>
			public virtual PackIndex.MutableEntry CloneEntry()
			{
				PackIndex.MutableEntry r = new PackIndex.MutableEntry();
				EnsureId();
				r.idBuffer.FromObjectId(idBuffer);
				r.offset = offset;
				return r;
			}

			internal virtual void EnsureId()
			{
			}
			// Override in implementations.
		}

		internal abstract class EntriesIterator : Iterator<PackIndex.MutableEntry>
		{
			protected internal readonly PackIndex.MutableEntry entry;

			protected internal long returnedNumber = 0;

			protected internal abstract PackIndex.MutableEntry InitEntry();
			
			public override bool HasNext()
			{
				return this.returnedNumber < this._enclosing.GetObjectCount();
			}

			/// <summary>
			/// Implementation must update
			/// <see cref="returnedNumber">returnedNumber</see>
			/// before returning
			/// element.
			/// </summary>
			public abstract override PackIndex.MutableEntry Next();

			public override void Remove()
			{
				throw new NotSupportedException();
			}

			internal EntriesIterator(PackIndex _enclosing)
			{
				entry = InitEntry();
				this._enclosing = _enclosing;
			}

			private readonly PackIndex _enclosing;
		}
	}
}