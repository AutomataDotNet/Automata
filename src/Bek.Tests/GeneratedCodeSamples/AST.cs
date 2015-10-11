using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Microsoft.Bek.Tests
{

    class BekException : Exception
    {
        public BekException(string message) { }
    }

    public static class CssEncode_AST
    {
        static char hex(char c)
        {
            return c;
        }

        static int hex(int i)
        {
            return i;
        }

        static char hex(char c1, char c2)
        {
            return c2;
        }
        static char ite(bool b, char c1, char c2)
        {
            if (b)
                return c1;
            else
                return c2;
        }

 public static string Apply (string input ) {
 StringBuilder ret = new StringBuilder();
 var strChars = input.ToCharArray();
var HS = false ;
var hs = '\0' ;
for (int i = 0; i < strChars.Length; i++) {
 char c = strChars[i];
if ( ((c == '\uFFFE') | (c == '\uFFFF'))  )
{
BekException ex = new BekException(" InvalidUnicodeValueException ");
throw ex;

}
if ( HS )
{
if ( !((c>='\uDC00') & (c<='\uDFFF'))  )
 { BekException ex = new BekException(" InvalidSurrogatePairException ");
throw ex;

 }
 else {
ret.Append( (char) hex(((hs<<'\x2')|((c>>'\b')&'\x3')))  );
ret.Append( (char) hex('\x1',c)  );
ret.Append( (char) hex(c)  );

HS = false ;

hs = '\0' ;


 }

}
if ( true  )
{
if ( ((c>='\uDC00') & (c<='\uDFFF'))  )
 { BekException ex = new BekException(" InvalidSurrogatePairException ");
throw ex;

 }
 else {
if ( ((c>='\uD800') & (c<='\uDBFF'))  )
 { ret.Append( (char) '\\'  );
ret.Append( (char) ite((((c>>'\x6')&'\xF') == '\xF'),'1','0')  );
ret.Append( (char) hex(((c>>'\x6')+'\x1'))  );
ret.Append( (char) hex((c>>'\x2'))  );
HS = true ;
hs = (char)(c&'\x3') ;

 }
 else {
if ( (c>'\u00FF')  )
 { ret.Append( (char) '\\'  );
ret.Append( (char) '0'  );
ret.Append( (char) '0'  );
ret.Append( (char) hex('\x3',c)  );
ret.Append( (char) hex('\x2',c)  );
ret.Append( (char) hex('\x1',c)  );
ret.Append( (char) hex(c)  );

 }
 else {
if ( (new Regex(" ^([0-9A-Za-z?-??-? -¥])$").IsMatch(c.ToString())  ))
 { ret.Append( (char) c );

 }
 else {
ret.Append( (char) '\\'  );
ret.Append( (char) '0'  );
ret.Append( (char) '0'  );
ret.Append( (char) '0'  );
ret.Append( (char) '0'  );
ret.Append( (char) hex('\x1',c)  );
ret.Append( (char) hex(c)  );


 }


 }


 }


 }

}
}


// End cases

if ( HS )
{
BekException ex = new BekException(" InvalidSurrogatePairException ");
throw ex;

}
if ( true  )
{

}

return ret.ToString();
}


    }

    public static class UTF8Encode_AST
    {
        public static bool IsLowSurrogate(char c)
        {
            return ((c >= 0xdc00) && (c <= 0xdfff));
        }

        public static bool IsHighSurrogate(char c)
        {
            return ((c >= 0xd800) && (c <= 0xdbff));
        }

        public static string Apply(string input)
        {
            StringBuilder ret = new StringBuilder();
            var strChars = input.ToCharArray();
            var HS = false;
            var hs = '\0';
            for (int i = 0; i < strChars.Length; i++)
            {
                int c = (int)strChars[i];
                if (HS)
                {
                    if (!((c >= '\uDC00') & (c <= '\uDFFF')))
                    {
                        BekException ex = new BekException(" InvalidSurrogatePairException ");
                        throw ex;

                    }
                    else
                    {
                        ret.Append((char)(('\u0080' | (hs << '\x4')) | ((c >> '\x6') & '\xF')));
                        ret.Append((char)('\u0080' | (c & '?')));

                        HS = false;

                        hs = '\0';


                    }

                }
                if (!HS)
                {
                    if ((c <= '\u007F'))
                    {
                        ret.Append((char)c);

                    }
                    else
                    {
                        if ((c <= '\u07FF'))
                        {
                            ret.Append((char)('\u00C0' | ((c >> '\x6') & '\x1F')));
                            ret.Append((char)('\u0080' | (c & '?')));

                        }
                        else
                        {
                            if (!((c >= '\uD800') & (c <= '\uDBFF')))
                            {
                                if (((c >= '\uDC00') & (c <= '\uDFFF')))
                                {
                                    BekException ex = new BekException(" InvalidSurrogatePairException ");
                                    throw ex;

                                }
                                else
                                {
                                    ret.Append((char)('\u00E0' | ((c >> '\f') & '\xF')));
                                    ret.Append((char)('\u0080' | ((c >> '\x6') & '?')));
                                    ret.Append((char)('\u0080' | (c & '?')));


                                }

                            }
                            else
                            {
                                ret.Append((char)('\u00F0' | ((('\x1' + ((c >> '\x6') & '\xF')) >> '\x2') & '\a')));
                                ret.Append((char)(('\u0080' | ((('\x1' + ((c >> '\x6') & '\xF')) & '\x3') << '\x4')) | ((c >>
                                '\x2') & '\xF')));

                                HS = true;

                                hs = (char)(c & '\x3');


                            }


                        }


                    }

                }
            }


            // End cases

            if (HS)
            {
                BekException ex = new BekException(" InvalidSurrogatePairException ");
                throw ex;

            }
            if (true)
            {

            }

            return ret.ToString();
        }


    }
}
