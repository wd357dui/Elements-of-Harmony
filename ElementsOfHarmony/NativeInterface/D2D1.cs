using System.Runtime.InteropServices;
using UnityEngine;

namespace ElementsOfHarmony.NativeInterface
{
	public static class D2D1
	{
		[StructLayout(LayoutKind.Sequential)]
		public struct D2D1_COLOR_F
		{
			public float R, G, B, A;
			public static implicit operator D2D1_COLOR_F(Color UnityColor) => new D2D1_COLOR_F
			{
				R = UnityColor.r,
				G = UnityColor.g,
				B = UnityColor.b,
				A = UnityColor.a,
			};
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct D2D1_POINT_2F
		{
			public float X, Y;
			public static implicit operator D2D1_POINT_2F(Vector2 V2) => new D2D1_POINT_2F
			{
				X = V2.x,
				Y = V2.y,
			};
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct D2D1_RECT_F
		{
			public float Left, Top, Right, Bottom;
			public static implicit operator D2D1_RECT_F(Rect UnityRect) => new D2D1_RECT_F
			{
				Left = UnityRect.xMin,
				Top = UnityRect.yMin,
				Right = UnityRect.xMax,
				Bottom = UnityRect.yMax,
			};
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct Matrix3x2F
		{
			public float
				M11, M12,
				M21, M22,
				M31, M32;
		}

		/// <summary>
		/// The font weight enumeration describes common values for degree of blackness or thickness of strokes of characters in a font.
		/// Font weight values less than 1 or greater than 999 are considered to be invalid, and they are rejected by font API functions.
		/// </summary>
		public enum DWRITE_FONT_WEIGHT
		{
			/// <summary>
			/// Predefined font weight : Thin (100).
			/// </summary>
			DWRITE_FONT_WEIGHT_THIN = 100,

			/// <summary>
			/// Predefined font weight : Extra-light (200).
			/// </summary>
			DWRITE_FONT_WEIGHT_EXTRA_LIGHT = 200,

			/// <summary>
			/// Predefined font weight : Ultra-light (200).
			/// </summary>
			DWRITE_FONT_WEIGHT_ULTRA_LIGHT = 200,

			/// <summary>
			/// Predefined font weight : Light (300).
			/// </summary>
			DWRITE_FONT_WEIGHT_LIGHT = 300,

			/// <summary>
			/// Predefined font weight : Semi-light (350).
			/// </summary>
			DWRITE_FONT_WEIGHT_SEMI_LIGHT = 350,

			/// <summary>
			/// Predefined font weight : Normal (400).
			/// </summary>
			DWRITE_FONT_WEIGHT_NORMAL = 400,

			/// <summary>
			/// Predefined font weight : Regular (400).
			/// </summary>
			DWRITE_FONT_WEIGHT_REGULAR = 400,

			/// <summary>
			/// Predefined font weight : Medium (500).
			/// </summary>
			DWRITE_FONT_WEIGHT_MEDIUM = 500,

			/// <summary>
			/// Predefined font weight : Demi-bold (600).
			/// </summary>
			DWRITE_FONT_WEIGHT_DEMI_BOLD = 600,

			/// <summary>
			/// Predefined font weight : Semi-bold (600).
			/// </summary>
			DWRITE_FONT_WEIGHT_SEMI_BOLD = 600,

			/// <summary>
			/// Predefined font weight : Bold (700).
			/// </summary>
			DWRITE_FONT_WEIGHT_BOLD = 700,

			/// <summary>
			/// Predefined font weight : Extra-bold (800).
			/// </summary>
			DWRITE_FONT_WEIGHT_EXTRA_BOLD = 800,

			/// <summary>
			/// Predefined font weight : Ultra-bold (800).
			/// </summary>
			DWRITE_FONT_WEIGHT_ULTRA_BOLD = 800,

			/// <summary>
			/// Predefined font weight : Black (900).
			/// </summary>
			DWRITE_FONT_WEIGHT_BLACK = 900,

			/// <summary>
			/// Predefined font weight : Heavy (900).
			/// </summary>
			DWRITE_FONT_WEIGHT_HEAVY = 900,

			/// <summary>
			/// Predefined font weight : Extra-black (950).
			/// </summary>
			DWRITE_FONT_WEIGHT_EXTRA_BLACK = 950,

			/// <summary>
			/// Predefined font weight : Ultra-black (950).
			/// </summary>
			DWRITE_FONT_WEIGHT_ULTRA_BLACK = 950,

			NULL = -1,
		};

		/// <summary>
		/// The font stretch enumeration describes relative change from the normal aspect ratio
		/// as specified by a font designer for the glyphs in a font.
		/// Values less than 1 or greater than 9 are considered to be invalid, and they are rejected by font API functions.
		/// </summary>
		public enum DWRITE_FONT_STRETCH
		{
			/// <summary>
			/// Predefined font stretch : Not known (0).
			/// </summary>
			DWRITE_FONT_STRETCH_UNDEFINED = 0,

			/// <summary>
			/// Predefined font stretch : Ultra-condensed (1).
			/// </summary>
			DWRITE_FONT_STRETCH_ULTRA_CONDENSED = 1,

			/// <summary>
			/// Predefined font stretch : Extra-condensed (2).
			/// </summary>
			DWRITE_FONT_STRETCH_EXTRA_CONDENSED = 2,

			/// <summary>
			/// Predefined font stretch : Condensed (3).
			/// </summary>
			DWRITE_FONT_STRETCH_CONDENSED = 3,

			/// <summary>
			/// Predefined font stretch : Semi-condensed (4).
			/// </summary>
			DWRITE_FONT_STRETCH_SEMI_CONDENSED = 4,

			/// <summary>
			/// Predefined font stretch : Normal (5).
			/// </summary>
			DWRITE_FONT_STRETCH_NORMAL = 5,

			/// <summary>
			/// Predefined font stretch : Medium (5).
			/// </summary>
			DWRITE_FONT_STRETCH_MEDIUM = 5,

			/// <summary>
			/// Predefined font stretch : Semi-expanded (6).
			/// </summary>
			DWRITE_FONT_STRETCH_SEMI_EXPANDED = 6,

			/// <summary>
			/// Predefined font stretch : Expanded (7).
			/// </summary>
			DWRITE_FONT_STRETCH_EXPANDED = 7,

			/// <summary>
			/// Predefined font stretch : Extra-expanded (8).
			/// </summary>
			DWRITE_FONT_STRETCH_EXTRA_EXPANDED = 8,

			/// <summary>
			/// Predefined font stretch : Ultra-expanded (9).
			/// </summary>
			DWRITE_FONT_STRETCH_ULTRA_EXPANDED = 9,

			NULL = -1,
		};

		/// <summary>
		/// The font style enumeration describes the slope style of a font face, such as Normal, Italic or Oblique.
		/// Values other than the ones defined in the enumeration are considered to be invalid, and they are rejected by font API functions.
		/// </summary>
		public enum DWRITE_FONT_STYLE
		{
			/// <summary>
			/// Font slope style : Normal.
			/// </summary>
			DWRITE_FONT_STYLE_NORMAL,

			/// <summary>
			/// Font slope style : Oblique.
			/// </summary>
			DWRITE_FONT_STYLE_OBLIQUE,

			/// <summary>
			/// Font slope style : Italic.
			/// </summary>
			DWRITE_FONT_STYLE_ITALIC,

			NULL = -1,

		};

		/// <summary>
		/// Alignment of paragraph text along the reading direction axis relative to 
		/// the leading and trailing edge of the layout box.
		/// </summary>
		public enum DWRITE_TEXT_ALIGNMENT
		{
			/// <summary>
			/// The leading edge of the paragraph text is aligned to the layout box's leading edge.
			/// </summary>
			DWRITE_TEXT_ALIGNMENT_LEADING,

			/// <summary>
			/// The trailing edge of the paragraph text is aligned to the layout box's trailing edge.
			/// </summary>
			DWRITE_TEXT_ALIGNMENT_TRAILING,

			/// <summary>
			/// The center of the paragraph text is aligned to the center of the layout box.
			/// </summary>
			DWRITE_TEXT_ALIGNMENT_CENTER,

			/// <summary>
			/// Align text to the leading side, and also justify text to fill the lines.
			/// </summary>
			DWRITE_TEXT_ALIGNMENT_JUSTIFIED,

			NULL = -1,
		};

		/// <summary>
		/// Alignment of paragraph text along the flow direction axis relative to the
		/// flow's beginning and ending edge of the layout box.
		/// </summary>
		public enum DWRITE_PARAGRAPH_ALIGNMENT
		{
			/// <summary>
			/// The first line of paragraph is aligned to the flow's beginning edge of the layout box.
			/// </summary>
			DWRITE_PARAGRAPH_ALIGNMENT_NEAR,

			/// <summary>
			/// The last line of paragraph is aligned to the flow's ending edge of the layout box.
			/// </summary>
			DWRITE_PARAGRAPH_ALIGNMENT_FAR,

			/// <summary>
			/// The center of the paragraph is aligned to the center of the flow of the layout box.
			/// </summary>
			DWRITE_PARAGRAPH_ALIGNMENT_CENTER,

			NULL = -1,
		};

		/// <summary>
		/// Word wrapping in multiline paragraph.
		/// </summary>
		public enum DWRITE_WORD_WRAPPING
		{
			/// <summary>
			/// Words are broken across lines to avoid text overflowing the layout box.
			/// </summary>
			DWRITE_WORD_WRAPPING_WRAP = 0,

			/// <summary>
			/// Words are kept within the same line even when it overflows the layout box.
			/// This option is often used with scrolling to reveal overflow text. 
			/// </summary>
			DWRITE_WORD_WRAPPING_NO_WRAP = 1,

			/// <summary>
			/// Words are broken across lines to avoid text overflowing the layout box.
			/// Emergency wrapping occurs if the word is larger than the maximum width.
			/// </summary>
			DWRITE_WORD_WRAPPING_EMERGENCY_BREAK = 2,

			/// <summary>
			/// Only wrap whole words, never breaking words (emergency wrapping) when the
			/// layout width is too small for even a single word.
			/// </summary>
			DWRITE_WORD_WRAPPING_WHOLE_WORD = 3,

			/// <summary>
			/// Wrap between any valid characters clusters.
			/// </summary>
			DWRITE_WORD_WRAPPING_CHARACTER = 4,

			NULL = -1,
		};

		/// <summary>
		/// Direction for how reading progresses.
		/// </summary>
		public enum DWRITE_READING_DIRECTION
		{
			/// <summary>
			/// Reading progresses from left to right.
			/// </summary>
			DWRITE_READING_DIRECTION_LEFT_TO_RIGHT = 0,

			/// <summary>
			/// Reading progresses from right to left.
			/// </summary>
			DWRITE_READING_DIRECTION_RIGHT_TO_LEFT = 1,

			/// <summary>
			/// Reading progresses from top to bottom.
			/// </summary>
			DWRITE_READING_DIRECTION_TOP_TO_BOTTOM = 2,

			/// <summary>
			/// Reading progresses from bottom to top.
			/// </summary>
			DWRITE_READING_DIRECTION_BOTTOM_TO_TOP = 3,

			NULL = -1,
		};

		/// <summary>
		/// Direction for how lines of text are placed relative to one another.
		/// </summary>
		public enum DWRITE_FLOW_DIRECTION
		{
			/// <summary>
			/// Text lines are placed from top to bottom.
			/// </summary>
			DWRITE_FLOW_DIRECTION_TOP_TO_BOTTOM = 0,

			/// <summary>
			/// Text lines are placed from bottom to top.
			/// </summary>
			DWRITE_FLOW_DIRECTION_BOTTOM_TO_TOP = 1,

			/// <summary>
			/// Text lines are placed from left to right.
			/// </summary>
			DWRITE_FLOW_DIRECTION_LEFT_TO_RIGHT = 2,

			/// <summary>
			/// Text lines are placed from right to left.
			/// </summary>
			DWRITE_FLOW_DIRECTION_RIGHT_TO_LEFT = 3,

			NULL = -1,
		};

		/// <summary>
		/// The method used for line spacing in layout.
		/// </summary>
		public enum DWRITE_LINE_SPACING_METHOD
		{
			/// <summary>
			/// Line spacing depends solely on the content, growing to accommodate the size of fonts and inline objects.
			/// </summary>
			DWRITE_LINE_SPACING_METHOD_DEFAULT,

			/// <summary>
			/// Lines are explicitly set to uniform spacing, regardless of contained font sizes.
			/// This can be useful to avoid the uneven appearance that can occur from font fallback.
			/// </summary>
			DWRITE_LINE_SPACING_METHOD_UNIFORM,

			/// <summary>
			/// Line spacing and baseline distances are proportional to the computed values based on the content, the size of the fonts and inline objects.
			/// </summary>
			DWRITE_LINE_SPACING_METHOD_PROPORTIONAL,

			NULL = -1,
		};
	}
}
