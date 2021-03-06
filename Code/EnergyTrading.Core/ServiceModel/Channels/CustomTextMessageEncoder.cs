﻿namespace EnergyTrading.ServiceModel.Channels
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.ServiceModel.Channels;
    using System.Text;
    using System.Xml;
    using System.Xml.Linq;

    using EnergyTrading.Xml.Serialization;

    /// <summary>
    /// Custom text encoder that can apply multiple transformations.
    /// </summary>
    public class CustomTextMessageEncoder : MessageEncoder
    {
        private static int i;
        private readonly CustomTextMessageEncoderFactory factory;
        private readonly XmlWriterSettings writerSettings;
        private readonly IList<IMessageTransformer> transformers;
        private string contentType;
        
        /// <summary>
        /// Creates a new instace of the <see cref="CustomTextMessageEncoder" /> class.
        /// </summary>
        /// <param name="factory">Factory to use</param>
        public CustomTextMessageEncoder(CustomTextMessageEncoderFactory factory)
        {
            this.factory = factory;
            
            this.writerSettings = new XmlWriterSettings { Encoding = Encoding.GetEncoding(factory.CharSet) };
            this.contentType = string.Format("{0}; charset={1}", this.factory.MediaType, this.writerSettings.Encoding.HeaderName);
            this.transformers = this.factory.Transformers;
        }

        /// <inheritdoc />
        public override string ContentType
        {
            get { return this.contentType; }
        }

        /// <inheritdoc />
        public override string MediaType
        {
            get { return this.factory.MediaType; }
        }

        /// <inheritdoc />
        public override MessageVersion MessageVersion
        {
            get { return this.factory.MessageVersion; }
        }

        /// <inheritdoc />
        public override Message ReadMessage(ArraySegment<byte> buffer, BufferManager bufferManager, string contentType)
        {
            // Align internal/external types
            this.contentType = contentType;

            var msgContents = new byte[buffer.Count];
            Array.Copy(buffer.Array, buffer.Offset, msgContents, 0, msgContents.Length);
            bufferManager.ReturnBuffer(buffer.Array);

            // Most interoperable to include the xml declaration
            this.writerSettings.OmitXmlDeclaration = false;

            // Save the encoding for when we write the response
            this.writerSettings.Encoding = msgContents.GetEncoding(contentType);
            var xmlDeclEncoding = msgContents.GetXmlDeclEncoding(this.writerSettings.Encoding);

            // Check if the two encodings align
            if (xmlDeclEncoding != null && xmlDeclEncoding.WebName == this.writerSettings.Encoding.WebName)
            {
                // Need to recode
                msgContents = Encoding.Convert(this.writerSettings.Encoding, xmlDeclEncoding, msgContents);
            }

            var stream = new MemoryStream(msgContents);
            return this.ReadMessage(stream, int.MaxValue);
        }

        /// <inheritdoc />
        public override Message ReadMessage(Stream stream, int maxSizeOfHeaders, string contentType)
        {
            var reader = XmlReader.Create(stream);
            return Message.CreateMessage(reader, maxSizeOfHeaders, this.MessageVersion);
        }

        /// <inheritdoc />
        public override ArraySegment<byte> WriteMessage(Message message, int maxMessageSize, BufferManager bufferManager, int messageOffset)
        {
            // Produce the basic stream
            var stream = new MemoryStream();
            message.XmlToStream(this.writerSettings, stream);

            // Apply the transformers
            stream = this.Transform(stream);

            // Now produce the final version of the stream.
            var messageBytes = stream.GetBuffer();
            var messageLength = (int)stream.Position;
            stream.Close();

            var totalLength = messageLength + messageOffset;
            var totalBytes = bufferManager.TakeBuffer(totalLength);
            Array.Copy(messageBytes, 0, totalBytes, messageOffset, messageLength);

            var byteArray = new ArraySegment<byte>(totalBytes, messageOffset, messageLength);
            return byteArray;
        }

        /// <inheritdoc />
        public override void WriteMessage(Message message, Stream stream)
        {
            message.XmlToStream(this.writerSettings, stream);
        }

        private MemoryStream Transform(Stream stream)
        {
            stream.Position = 0;
            var document = XDocument.Load(stream);

            // Apply the transformers
            foreach (var transformer in this.transformers)
            {
                transformer.Transform(document);
            }

            // NOTE: WCF message logging doesn't help as we can't see the content of streamed messages
            // TODO: Need a better strategy here - IMessgeLogger?
            document.Save(string.Format(@"C:\Devel\RegulatoryReporting\messag{0:0000}.xml", ++i));
            return document.WriteTo(this.writerSettings);
        }
    }
}