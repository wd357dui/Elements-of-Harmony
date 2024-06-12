using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace ElementsOfHarmony.DreamProject
{
	/// <summary>
	/// code refitted from my game engine called DreamProject <br/>
	/// (it's shitty in terms of readability for my today's standards,
	/// but I needed something that is ready to use to save time)
	/// </summary>
	public class Animator<FrameType>
	{
		public enum Seek
		{
			Forward, Backward, Adjacent, Initial
		}
		public Seek SeekMethod = Seek.Forward;

		public readonly SortedList<decimal, FrameType> Frames = new SortedList<decimal, FrameType>();

		private int Lower = 0;
		private int Upper = 0;

		/// <returns>true if interop is needed</returns>
		public bool UpdateFrame(decimal Timestamp, Seek? Method, out object? A, out object? B, out float Progress)
		{
			A = B = default;
			Progress = 0.0f;
			Seek method = Method ?? SeekMethod;
			bool Interop;
			switch (method)
			{
				case Seek.Forward:
					Interop = ForwardSeek(Timestamp, out A, out B, out Progress);
					break;
				case Seek.Backward:
					Interop = BackwardSeek(Timestamp, out A, out B, out Progress);
					break;
				case Seek.Adjacent:
					Interop = AdjacentSeek(Timestamp, out A, out B, out Progress);
					break;
				case Seek.Initial:
					Interop = InitialSeek(Timestamp, out A, out B, out Progress);
					break;
				default:
					return false;
			}
			return Interop;
		}

		public void Clear()
		{
			Frames.Clear();
			Upper = Lower = 0;
		}

		/// <summary>
		/// use binary search to find left and right key frames
		/// </summary>
		public bool InitialSeek(decimal Timestamp, out object? A, out object? B, out float Progress)
		{
			if (!AssertFrameCount(out A, out B, out Progress)) return false;
			else
			{
				// binary search
				Lower = Frames.Keys.ReverseBinarySearch(Timestamp);
				if (Lower < 0)
				{
					Lower = ~Lower;
				}
				Upper = Frames.Keys.BinarySearch(Lower, Frames.Count - Lower, Timestamp, Comparer<decimal>.Default);
				if (Upper < 0)
				{
					Upper = ~Upper;
				}

				if (AssertOnTarget(Timestamp, out A, out B, out Progress)) return false;
				else if (AssertInRange(Timestamp, out A, out B, out Progress)) return true;
				else if (Lower == 0)
				{
					OutOfBounds_TimestampIsEarlierThanFirstFrame(out A, out B, out Progress);
					return false;
				}
				else if (Upper == Frames.Count)
				{
					OutOfBounds_TimestampIsLaterThanLastFrame(out A, out B, out Progress);
					return false;
				}
				else
				{
					// there are no other cases, so code shouldn't be able to reach here
					throw new Exception("shouldn't be able to reach here");
				}
			}
		}

		/// <summary>
		/// traverse forward, one key frame at a time, to find left and right key frames
		/// </summary>
		public bool ForwardSeek(decimal Timestamp, out object? A, out object? B, out float Progress)
		{
			if (!AssertFrameCount(out A, out B, out Progress)) return false;
			else
			{
			forward_seek_begin:
				if (AssertOnTarget(Timestamp, out A, out B, out Progress)) return false;
				else if (AssertInRange(Timestamp, out A, out B, out Progress)) return true;
				else if (Timestamp > Frames.Keys[Upper])
				{
					if (Upper + 1 >= Frames.Keys.Count)
					{
						OutOfBounds_TimestampIsLaterThanLastFrame(out A, out B, out Progress);
						return false;
					}
					else
					{
						// forward seeking
						if (Forward(out A, out B, out Progress)) goto forward_seek_begin;
						else return false;
					}
				}
				else
				{
					// wrong direction
					if (Lower > 0)
					{
						// go back to the start
						Lower = 0;
						Upper = Lower;
						goto forward_seek_begin;
					}
					else
					{
						OutOfBounds_TimestampIsEarlierThanFirstFrame(out A, out B, out Progress);
						return false;
					}
				}
			}
		}

		/// <summary>
		/// traverse backward, one key frame at a time, to find left and right key frames
		/// </summary>
		public bool BackwardSeek(decimal Timestamp, out object? A, out object? B, out float Progress)
		{
			if (!AssertFrameCount(out A, out B, out Progress)) return false;
			else
			{
			backward_seek_begin:
				if (AssertOnTarget(Timestamp, out A, out B, out Progress)) return false;
				else if (AssertInRange(Timestamp, out A, out B, out Progress)) return true;
				else if (Timestamp < Frames.Keys[Lower])
				{
					if (Lower == 0)
					{
						OutOfBounds_TimestampIsEarlierThanFirstFrame(out A, out B, out Progress);
						return false;
					}
					else
					{
						// backward seeking
						if (Backward(out A, out B, out Progress)) goto backward_seek_begin;
						else return false;
					}
				}
				else
				{
					// wrong direction
					if (Upper < Frames.Count - 1)
					{
						// go back to the end
						Upper = Frames.Count - 1;
						Lower = Upper;
						goto backward_seek_begin;
					}
					else
					{
						OutOfBounds_TimestampIsLaterThanLastFrame(out A, out B, out Progress);
						return false;
					}
				}
			}
		}

		/// <summary>
		/// traverse toward the direction of the target timestamp, one key frame at a time, to find left and right key frames
		/// </summary>
		public bool AdjacentSeek(decimal Timestamp, out object? A, out object? B, out float Progress)
		{
			if (!AssertFrameCount(out A, out B, out Progress)) return false;
			else
			{
			adjacent_seek_begin:
				if (AssertOnTarget(Timestamp, out A, out B, out Progress)) return false;
				else if (AssertInRange(Timestamp, out A, out B, out Progress)) return true;
				else if (Frames.Keys[Upper] < Timestamp)
				{
					if (Upper + 1 >= Frames.Keys.Count)
					{
						OutOfBounds_TimestampIsLaterThanLastFrame(out A, out B, out Progress);
						return false;
					}
					else
					{
						if (Forward(out A, out B, out Progress)) goto adjacent_seek_begin;
						else return false;
					}
				}
				else if (Frames.Keys[Lower] > Timestamp)
				{
					if (Lower == 0)
					{
						OutOfBounds_TimestampIsEarlierThanFirstFrame(out A, out B, out Progress);
						return false;
					}
					else
					{
						if (Backward(out A, out B, out Progress)) goto adjacent_seek_begin;
						else return false;
					}
				}
				else
				{
					// there are no other cases, so code shouldn't be able to reach here
					throw new Exception("shouldn't be able to reach here");
					// possible reasons:
					// 1. timestamp is NAN, which should have been handled earlier but wasn't
					// 2. AssertOnTarget should match but didn't
				}
			}
		}

		/// <returns>true if there are no less than 2 frames</returns>
		private bool AssertFrameCount(out object? A, out object? B, out float Progress)
		{
			A = B = default;
			Progress = 0;
			if (Frames.Count == 0) return false;
			else if (Frames.Count == 1)
			{
				A = B = Frames.Values[0];
				Progress = 0.0f;
				return false;
			}
			else return true;
		}
		/// <returns>true if any iterator is on target</returns>
		private bool AssertOnTarget(decimal Timestamp, out object? A, out object? B, out float Progress)
		{
			A = B = default;
			Progress = 0;
			if (Frames.Keys[Upper] == Timestamp)
			{
				A = B = Frames.Values[Upper];
				Progress = 0.0f;
				return true;
			}
			else if (Frames.Keys[Lower] == Timestamp)
			{
				A = B = Frames.Values[Lower];
				Progress = 0.0f;
				return true;
			}
			return false;
		}
		/// <returns>true if iterators are in range</returns>
		private bool AssertInRange(decimal Timestamp, out object? A, out object? B, out float Progress)
		{
			A = B = default;
			Progress = 0;
			if (Timestamp > Frames.Keys[Lower] && Timestamp < Frames.Keys[Upper])
			{
				A = Frames.Values[Lower];
				B = Frames.Values[Upper];
				Progress = (float)((Timestamp - Frames.Keys[Lower]) / (Frames.Keys[Upper] - Frames.Keys[Lower]));
				return true;
			}
			else return false;
		}
		/// <returns>true if stepped forward, false if reached the end</returns>
		private bool Forward(out object? A, out object? B, out float Progress)
		{
			A = B = default;
			Progress = 0;
			if (Upper < Frames.Count)
			{
				Lower = Upper;
				Upper++;
				return true;
			}
			else
			{
				A = B = Frames.Values[Upper];
				Progress = 0.0f;
				return false;
			}
		}
		/// <returns>true if stepped backward, false if reached the end</returns>
		private bool Backward(out object? A, out object? B, out float Progress)
		{
			A = B = default;
			Progress = 0;
			if (Lower > 0)
			{
				Upper = Lower;
				Lower--;
				return true;
			}
			else
			{
				A = B = Frames.Values[Lower];
				Progress = 0.0f;
				return false;
			}
		}
		private void OutOfBounds_TimestampIsEarlierThanFirstFrame(out object? A, out object? B, out float Progress)
		{
			A = B = Frames.Values.First();
			Progress = 0.0f;
		}
		private void OutOfBounds_TimestampIsLaterThanLastFrame(out object? A, out object? B, out float Progress)
		{
			A = B = Frames.Values.Last();
			Progress = 0.0f;
		}
	}
	public static class Algorithm
	{
		/// <summary>
		/// equivalent of std::upper_bound(),
		/// stolen from System.Collections.Generic.ArraySortHelper.InternalBinarySearch()<br/>
		/// really, I see no point in requiring the `array` arg to be an array, just anything with a int indexer should do
		/// </summary>
		/// <returns> (document stolen and modified from msdn)
		/// The zero-based index of item in the sorted IList, if item is found;<br/>
		/// otherwise, a negative number that is the bitwise complement of the index of the next element that is larger than item
		/// or, if there is no larger element, the bitwise complement of Count
		/// </returns>
		public static int BinarySearch<ValueType>(this IList<ValueType> array, int index, int length, ValueType value, IComparer<ValueType> comparer)
		{
			int first = index;
			int last = index + length - 1;
			while (first <= last)
			{
				int current = first + ((last - first) >> 1);
				int result = comparer.Compare(array[current], value);
				if (result == 0)
				{
					return current;
				}
				if (result < 0)
				{
					first = current + 1;
				}
				else
				{
					last = current - 1;
				}
			}
			return ~first;
		}

		/// <summary>
		/// equivalent of std::upper_bound(),
		/// stolen from System.Collections.Generic.ArraySortHelper.InternalBinarySearch()<br/>
		/// really, I see no point in requiring the `array` arg to be an array, just anything with a int indexer should do
		/// </summary>
		/// <returns> (document stolen and modified from msdn)
		/// The zero-based index of item in the sorted IList, if item is found;<br/>
		/// otherwise, a negative number that is the bitwise complement of the index of the next element that is larger than item
		/// or, if there is no larger element, the bitwise complement of Count
		/// </returns>
		public static int BinarySearch<ValueType>(this IList<ValueType> array, ValueType value, [Optional] IComparer<ValueType>? comparer)
		{
			return BinarySearch(array, 0, array.Count, value, comparer ?? Comparer<ValueType>.Default);
		}

		/// <summary>
		/// equivalent of std::lower_bound(), 
		/// stolen from System.Collections.Generic.ArraySortHelper.InternalBinarySearch().<br/>
		/// really, I see no point in requiring the `array` arg to be an array, just anything with a int indexer should do
		/// </summary>
		/// <returns> (document stolen and modified from msdn)
		/// The zero-based index of item in the sorted IList, if item is found;<br/>
		/// otherwise, a negative number that is the bitwise complement of the index of the next element that is no larger than item
		/// or, the bitwise complement of Count
		/// </returns>
		public static int ReverseBinarySearch<ValueType>(this IList<ValueType> array, int index, int length, ValueType value, IComparer<ValueType>? comparer = null)
		{
			comparer ??= Comparer<ValueType>.Default;
			int first = index;
			int last = index + length - 1;
			while (first <= last)
			{
				int current = first + ((last - first) >> 1);
				int result = comparer.Compare(array[current], value);
				if (result == 0)
				{
					return current;
				}
				if (result > 0)
				{
					last = current - 1;
				}
				else
				{
					first = current + 1;
				}
			}
			return ~first;
		}

		/// <summary>
		/// equivalent of std::lower_bound(), 
		/// stolen from System.Collections.Generic.ArraySortHelper.InternalBinarySearch().<br/>
		/// really, I see no point in requiring the `array` arg to be an array, just anything with a int indexer should do
		/// </summary>
		/// <returns> (document stolen and modified from msdn)
		/// The zero-based index of item in the sorted IList, if item is found;<br/>
		/// otherwise, a negative number that is the bitwise complement of the index of the next element that is no larger than item
		/// or, the bitwise complement of Count
		/// </returns>
		public static int ReverseBinarySearch<ValueType>(this IList<ValueType> array, ValueType value, [Optional] IComparer<ValueType>? comparer)
		{
			return BinarySearch(array, 0, array.Count, value, comparer ?? Comparer<ValueType>.Default);
		}
	}
}
