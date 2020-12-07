
using System;
using System.Runtime.Serialization;

namespace system
{

    public class Uri : ISerializable
    {

        [java.attr.RetainType] private java.net.URI JavaURI;

        //
        // Constructor
        //

        public Uri(string uriString, bool dontEscape, UriKind uriKind)
        {
            ThrowHelper.ThrowIfNull(uriString);
            if (uriString == "")
                throw new UriFormatException("empty URI");

            if (! dontEscape)
            {
                if (uriKind != UriKind.Relative)
                    uriString = java.net.URLEncoder.encode(uriString, "UTF-8");
            }

            try
            {
                JavaURI = new java.net.URI(uriString);
            }
            catch (java.net.URISyntaxException e)
            {
                throw new UriFormatException(e.getMessage(), e);
            }

            if (uriKind == UriKind.Absolute)
            {
                if (! JavaURI.isAbsolute())
                    throw new UriFormatException("relative URI for " + uriKind);
            }
            else if (uriKind == UriKind.Relative)
            {
                if (JavaURI.isAbsolute())
                    throw new UriFormatException("absolute URI for " + uriKind);
            }
            else if (uriKind != UriKind.RelativeOrAbsolute)
            {
                throw new ArgumentException();
            }
        }

        //
        // Constructors
        //

        public Uri(string uriString)
            : this(uriString, false, UriKind.Absolute) { }

        public Uri(string uriString, bool dontEscape)
            : this(uriString, dontEscape, UriKind.Absolute) { }

        public Uri(string uriString, UriKind uriKind)
            : this(uriString, false, uriKind) { }

        //
        // Constructor
        //

        public Uri(Uri baseUri, Uri relativeUri)
        {
            ThrowHelper.ThrowIfNull(baseUri);
            if (! baseUri.IsAbsoluteUri)
                throw new ArgumentOutOfRangeException();

            try
            {
                JavaURI = baseUri.JavaURI.resolve(relativeUri.JavaURI);
            }
            catch (java.net.URISyntaxException e)
            {
                throw new UriFormatException(e.getMessage(), e);
            }
        }

        public Uri(Uri baseUri, string relativeUri, bool dontEscape)
            : this(baseUri, new Uri(relativeUri, dontEscape)) { }

        //
        // Methods
        //

        public override string ToString() => JavaURI.ToString();
        public override int GetHashCode() => JavaURI.GetHashCode();
        public override bool Equals(object other)
        {
            var otherAsUri = other as Uri;
            return (((object) otherAsUri) != null && JavaURI.Equals(otherAsUri.JavaURI));
        }
        public static bool operator ==(Uri uri1, Uri uri2)
            => (uri1 == null) ? (uri2 == null) : uri1.Equals(uri2);
        public static bool operator !=(Uri uri1, Uri uri2) => ! (uri1 == uri2);


        //
        // Properties
        //

        public   bool IsAbsoluteUri => JavaURI.isAbsolute();

        public string AbsolutePath  => AbsoluteUriOrThrow.getPath();
        public string AbsoluteUri   => AbsoluteUriOrThrow.ToString();
        public string LocalPath     => AbsoluteUriOrThrow.getPath();
        public string Authority     => AbsoluteUriOrThrow.getAuthority();
        public string Host          => AbsoluteUriOrThrow.getHost();
        public string Query         => AbsoluteUriOrThrow.getQuery();
        public string Fragment      => AbsoluteUriOrThrow.getFragment();
        public string Scheme        => AbsoluteUriOrThrow.getScheme();
        public string UserInfo      => AbsoluteUriOrThrow.getUserInfo();
        public    int Port          => AbsoluteUriOrThrow.getPort();
        public   bool IsDefaultPort => Port == -1;
        public   bool IsFile        => Scheme == "file";
        public   bool IsUnc         => AbsoluteUriOrThrow.getPath()?.StartsWith("\\") ?? false;

        private java.net.URI AbsoluteUriOrThrow
            => JavaURI.isAbsolute() ? JavaURI
             : throw new InvalidOperationException("not absolute URI");

        //
        // ISerializable
        //

        public void GetObjectData(SerializationInfo info, StreamingContext context)
            => throw new PlatformNotSupportedException();

    }
}
