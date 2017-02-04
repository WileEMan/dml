using System;
using System.Collections.Generic;
using System.Text;

namespace WileyBlack.Dml.FileFormats
{
    public class DmlVideo1Translation : DmlTranslation
    {
        public string urn = "urn:dml:video1";
        public PrimitiveSet[] RequiredPrimitiveSets = new PrimitiveSet[] {
		    new PrimitiveSet("arrays", "le", null),
		    new PrimitiveSet("common", "le", null)
		};

        public class typeVideo : Association
        {
            public typeVideo()
                : base(1120, "Video", null)
            {
                LocalTranslation = DmlTranslation.CreateFrom(new Association[] {
				    VideoID,
				    Title,
				    Author,
				    Description,
				    Publisher,
				    Copyright,
				    StreamDescriptions,
				    Content
			    });
            }

            public Association VideoID = new Association(20, "VideoID", PrimitiveTypes.UInt);
            public Association Title = new Association(21, "Title", PrimitiveTypes.String);
            public Association Author = new Association(22, "Author", PrimitiveTypes.String);
            public Association Description = new Association(23, "Description", PrimitiveTypes.String);
            public Association Publisher = new Association(24, "Publisher", PrimitiveTypes.String);
            public Association Copyright = new Association(25, "Copyright", PrimitiveTypes.String);

            public class typeStreamDescriptions : Association
            {
                public typeStreamDescriptions()
                    : base(1, "Stream-Descriptions", null)
                {
                    LocalTranslation = DmlTranslation.CreateFrom(new Association[] {
					    StreamID,
					    Language,
					    Video,
					    Audio,
					    Subtitle
				    });
                }

                public Association StreamID = new Association(20, "StreamID", PrimitiveTypes.UInt);
                public Association Language = new Association(21, "Language", PrimitiveTypes.String);

                public class typeVideo : Association
                {
                    public typeVideo()
                        : base(1, "Video", null)
                    {
                        LocalTranslation = DmlTranslation.CreateFrom(new Association[] {
						    Width,
						    Height,
						    Format,
						    BitsPerPixel,
						    Interlaced,
						    CropLeft,
						    CropTop,
						    CropRight,
						    CropBottom,
						    PhysicalWidth,
						    PhysicalHeight,
						    FramesPerSecond
					    });
                    }

                    public Association Width = new Association(22, "Width", PrimitiveTypes.UInt);
                    public Association Height = new Association(23, "Height", PrimitiveTypes.UInt);
                    public Association Format = new Association(24, "Format", PrimitiveTypes.String);
                    public Association BitsPerPixel = new Association(25, "BitsPerPixel", PrimitiveTypes.UInt);
                    public Association Interlaced = new Association(26, "Interlaced", PrimitiveTypes.Boolean);
                    public Association CropLeft = new Association(27, "CropLeft", PrimitiveTypes.UInt);
                    public Association CropTop = new Association(28, "CropTop", PrimitiveTypes.UInt);
                    public Association CropRight = new Association(29, "CropRight", PrimitiveTypes.UInt);
                    public Association CropBottom = new Association(30, "CropBottom", PrimitiveTypes.UInt);
                    public Association PhysicalWidth = new Association(31, "PhysicalWidth", PrimitiveTypes.String);
                    public Association PhysicalHeight = new Association(32, "PhysicalHeight", PrimitiveTypes.String);
                    public Association FramesPerSecond = new Association(33, "FramesPerSecond", PrimitiveTypes.Single);
                }
                public typeVideo Video = new typeVideo();


                public class typeAudio : Association
                {
                    public typeAudio()
                        : base(2, "Audio", null)
                    {
                        LocalTranslation = DmlTranslation.CreateFrom(new Association[] {
						    SamplingFrequency,
						    SourceAzimuth,
						    SourceElevation,
						    BitDepth
					    });
                    }

                    public Association SamplingFrequency = new Association(22, "SamplingFrequency", PrimitiveTypes.UInt);
                    public Association SourceAzimuth = new Association(23, "Source-Azimuth", PrimitiveTypes.Single);
                    public Association SourceElevation = new Association(24, "Source-Elevation", PrimitiveTypes.Single);
                    public Association BitDepth = new Association(25, "BitDepth", PrimitiveTypes.UInt);
                }
                public typeAudio Audio = new typeAudio();

                public Association Subtitle = new Association(3, "Subtitle", NodeTypes.Container);
            }
            public typeStreamDescriptions StreamDescriptions = new typeStreamDescriptions();


            public class typeContent : Association
            {
                public typeContent()
                    : base(2, "Content", null)
                {
                    LocalTranslation = DmlTranslation.CreateFrom(new Association[] {
					    Index,
					    Block
				    });
                }


                public class typeIndex : Association
                {
                    public typeIndex()
                        : base(1, "Index", null)
                    {
                        LocalTranslation = DmlTranslation.CreateFrom(new Association[] {
						    Position,
						    BlockID,
						    NextIndexPosition,
                            FrameID,
                            FramesPerBlock,
						    BlockReference
					    });
                    }

                    public Association Position = new Association(20, "Position", PrimitiveTypes.UInt);
                    public Association BlockID = new Association(21, "BlockID", PrimitiveTypes.UInt);
                    public Association NextIndexPosition = new Association(22, "Next-Index-Position", PrimitiveTypes.UInt);
                    public Association FrameID = new Association(23, "FrameID", PrimitiveTypes.UInt);
                    public Association FramesPerBlock = new Association(24, "Frames-Per-Block", PrimitiveTypes.UInt);

                    public class typeBlockReference : Association
                    {
                        public typeBlockReference()
                            : base(1, "Block-Reference", null)
                        {
                            LocalTranslation = DmlTranslation.CreateFrom(new Association[] {
							    DeltaPosition,
							    StartTime,
							    Duration,
                                NFrames
						    });
                        }

                        public Association DeltaPosition = new Association(23, "Delta-Position", PrimitiveTypes.Int);
                        public Association StartTime = new Association(24, "StartTime", PrimitiveTypes.UInt);
                        public Association Duration = new Association(25, "Duration", PrimitiveTypes.UInt);
                        public Association NFrames = new Association(26, "NFrames", PrimitiveTypes.UInt);
                    }
                    public typeBlockReference BlockReference = new typeBlockReference();

                }
                public typeIndex Index = new typeIndex();


                public class typeBlock : Association
                {
                    public typeBlock()
                        : base(2, "Block", null)
                    {
                        LocalTranslation = DmlTranslation.CreateFrom(new Association[] {
						    StreamID,
						    VideoFrame,
						    AudioFrame,
						    SubtitleText
					    });
                    }

                    public Association StreamID = new Association(20, "StreamID", PrimitiveTypes.UInt);

                    public class typeVideoFrame : Association
                    {
                        public typeVideoFrame()
                            : base(1, "Video-Frame", null)
                        {
                            LocalTranslation = DmlTranslation.CreateFrom(new Association[] {
							    BitmapU8,
							    BitmapU16,
							    BitmapU24,
							    BitmapU32,
							    BitmapSF,
							    BitmapDF,
                                StartTime,
                                Duration
						    });
                        }

                        public Association BitmapU8 = new Association(1, "Bitmap", PrimitiveTypes.Matrix, ArrayTypes.U8);
                        public Association BitmapU16 = new Association(2, "Bitmap", PrimitiveTypes.Matrix, ArrayTypes.U16);
                        public Association BitmapU24 = new Association(3, "Bitmap", PrimitiveTypes.Matrix, ArrayTypes.U24);
                        public Association BitmapU32 = new Association(4, "Bitmap", PrimitiveTypes.Matrix, ArrayTypes.U32);
                        public Association BitmapSF = new Association(5, "Bitmap", PrimitiveTypes.Matrix, ArrayTypes.Singles);
                        public Association BitmapDF = new Association(6, "Bitmap", PrimitiveTypes.Matrix, ArrayTypes.Doubles);
                        public Association StartTime = new Association(24, "StartTime", PrimitiveTypes.UInt);
                        public Association Duration = new Association(25, "Duration", PrimitiveTypes.UInt);
                    }
                    public typeVideoFrame VideoFrame = new typeVideoFrame();

                    public Association AudioFrame = new Association(2, "Audio-Frame", NodeTypes.Container);

                    public class typeSubtitleText : Association
                    {
                        public typeSubtitleText()
                            : base(3, "Subtitle-Text", null)
                        {
                            LocalTranslation = DmlTranslation.CreateFrom(new Association[] {
							    Line
						    });
                        }


                        public class typeLine : Association
                        {
                            public typeLine()
                                : base(1, "Line", null)
                            {
                                LocalTranslation = DmlTranslation.CreateFrom(new Association[] {
								    Value
							    });
                            }

                            public Association Value = new Association(21, "Value", PrimitiveTypes.String);
                        }
                        public typeLine Line = new typeLine();

                    }
                    public typeSubtitleText SubtitleText = new typeSubtitleText();

                }
                public typeBlock Block = new typeBlock();

            }
            public typeContent Content = new typeContent();

        }
        public typeVideo Video = new typeVideo();


        public DmlVideo1Translation()
            : base()
        {
            Add(EC2);
            Add(new Association[] {
			    Video
		    });
        }

        public static DmlVideo1Translation V1 = new DmlVideo1Translation();
    }
}
