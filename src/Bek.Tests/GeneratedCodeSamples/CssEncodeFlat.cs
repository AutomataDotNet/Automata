using System;
using System.Text;
using System.Collections.Generic;

namespace Microsoft.Bek.Tests {
public class CssEncoder : Microsoft.Automata.IDeterministicFiniteTransducer
{
static int Bits(int m, int n, int c){return ((c >> n) & ~(-2 << (m - n)));}

public static string Apply(string input){
var output = new StringBuilder(input.Length >= 16384 ? input.Length : (int)(Math.Min(16384, (long)input.Length * 7)));
int state = 0;
for (int i = 0; i < input.Length; i++){
int c = (int)input[i];
switch (state){
case (0): {
  if (((c==0xFFFE)||(c==0xFFFF))) {
    throw new Exception("CssEncoder");
  } else {
    if (((0xDC00<=c)&&(c<=0xDFFF))) {
      throw new Exception("CssEncoder");
    } else {
      if (((0xD800<=c)&&(c<=0xDBFF))) {
        if ((Bits(1,0,c)==0)) {
          output.Append((char)0x5C);
          output.Append((char)(Bits(9,6,c)==0xF ? 0x31 : 0x30));
          output.Append((char)(((1+Bits(15,6,c))&0xF)+(((1+Bits(15,6,c))&0xF)<=9 ? 48 : 55)));
          output.Append((char)((Bits(15,2,c)&0xF)+((Bits(15,2,c)&0xF)<=9 ? 48 : 55)));
          state = 4;
        } else {
          if ((Bits(1,0,c)==2)) {
            output.Append((char)0x5C);
            output.Append((char)(Bits(9,6,c)==0xF ? 0x31 : 0x30));
            output.Append((char)(((1+Bits(15,6,c))&0xF)+(((1+Bits(15,6,c))&0xF)<=9 ? 48 : 55)));
            output.Append((char)((Bits(15,2,c)&0xF)+((Bits(15,2,c)&0xF)<=9 ? 48 : 55)));
            state = 3;
          } else {
            if ((Bits(1,0,c)==3)) {
              output.Append((char)0x5C);
              output.Append((char)(Bits(9,6,c)==0xF ? 0x31 : 0x30));
              output.Append((char)(((1+Bits(15,6,c))&0xF)+(((1+Bits(15,6,c))&0xF)<=9 ? 48 : 55)));
              output.Append((char)((Bits(15,2,c)&0xF)+((Bits(15,2,c)&0xF)<=9 ? 48 : 55)));
              state = 2;
            } else {
              output.Append((char)0x5C);
              output.Append((char)(Bits(9,6,c)==0xF ? 0x31 : 0x30));
              output.Append((char)(((1+Bits(15,6,c))&0xF)+(((1+Bits(15,6,c))&0xF)<=9 ? 48 : 55)));
              output.Append((char)((Bits(15,2,c)&0xF)+((Bits(15,2,c)&0xF)<=9 ? 48 : 55)));
              state = 1;
            }
          }
        }
      } else {
        if (!(Bits(15,8,c)==0)) {
          output.Append((char)0x5C);
          output.Append((char)0x30);
          output.Append((char)0x30);
          output.Append((char)(((c>>12)&0xF)+(((c>>12)&0xF)<=9 ? 48 : 55)));
          output.Append((char)(((c>>8)&0xF)+(((c>>8)&0xF)<=9 ? 48 : 55)));
          output.Append((char)(((c>>4)&0xF)+(((c>>4)&0xF)<=9 ? 48 : 55)));
          output.Append((char)((c&0xF)+((c&0xF)<=9 ? 48 : 55)));
          state = 0;
        } else {
          if ((((0x30<=c)&&(Bits(15,6,c)==0)&&(Bits(5,0,c)<=0x39))||((0x41<=c)&&(Bits(15,7,c)==0)&&(Bits(6,0,c)<=0x5A))||((0x61<=c)&&(Bits(15,7,c)==0)&&(Bits(6,0,c)<=0x7A))||((0x80<=c)&&(Bits(15,8,c)==0)&&(Bits(7,0,c)<=0x90))||((0x93<=c)&&(Bits(15,8,c)==0)&&(Bits(7,0,c)<=0x9A))||((0xA0<=c)&&(Bits(15,8,c)==0)&&(Bits(7,0,c)<=0xA5)))) {
            output.Append((char)c);
            state = 0;
          } else {
            output.Append((char)0x5C);
            output.Append((char)0x30);
            output.Append((char)0x30);
            output.Append((char)0x30);
            output.Append((char)0x30);
            output.Append((char)(((c>>4)&0xF)+(((c>>4)&0xF)<=9 ? 48 : 55)));
            output.Append((char)((c&0xF)+((c&0xF)<=9 ? 48 : 55)));
            state = 0;
          }
        }
      }
    }
  }
break;}
case (4): {
  if (((c==0xFFFE)||(c==0xFFFF))) {
    throw new Exception("CssEncoder");
  } else {
    if (((0xDC00>c)||(c>0xDFFF))) {
      throw new Exception("CssEncoder");
    } else {
      output.Append((char)((Bits(9,8,c)&0xF)+((Bits(9,8,c)&0xF)<=9 ? 48 : 55)));
      output.Append((char)(((c>>4)&0xF)+(((c>>4)&0xF)<=9 ? 48 : 55)));
      output.Append((char)((c&0xF)+((c&0xF)<=9 ? 48 : 55)));
      state = 0;
    }
  }
break;}
case (3): {
  if (((c==0xFFFE)||(c==0xFFFF))) {
    throw new Exception("CssEncoder");
  } else {
    if (((0xDC00>c)||(c>0xDFFF))) {
      throw new Exception("CssEncoder");
    } else {
      output.Append((char)(((8|Bits(9,8,c))&0xF)+(((8|Bits(9,8,c))&0xF)<=9 ? 48 : 55)));
      output.Append((char)(((c>>4)&0xF)+(((c>>4)&0xF)<=9 ? 48 : 55)));
      output.Append((char)((c&0xF)+((c&0xF)<=9 ? 48 : 55)));
      state = 0;
    }
  }
break;}
case (2): {
  if (((c==0xFFFE)||(c==0xFFFF))) {
    throw new Exception("CssEncoder");
  } else {
    if (((0xDC00>c)||(c>0xDFFF))) {
      throw new Exception("CssEncoder");
    } else {
      output.Append((char)(((0xC|Bits(9,8,c))&0xF)+(((0xC|Bits(9,8,c))&0xF)<=9 ? 48 : 55)));
      output.Append((char)(((c>>4)&0xF)+(((c>>4)&0xF)<=9 ? 48 : 55)));
      output.Append((char)((c&0xF)+((c&0xF)<=9 ? 48 : 55)));
      state = 0;
    }
  }
break;}
case (1): {
  if (((c==0xFFFE)||(c==0xFFFF))) {
    throw new Exception("CssEncoder");
  } else {
    if (((0xDC00>c)||(c>0xDFFF))) {
      throw new Exception("CssEncoder");
    } else {
      output.Append((char)(((4|Bits(9,8,c))&0xF)+(((4|Bits(9,8,c))&0xF)<=9 ? 48 : 55)));
      output.Append((char)(((c>>4)&0xF)+(((c>>4)&0xF)<=9 ? 48 : 55)));
      output.Append((char)((c&0xF)+((c&0xF)<=9 ? 48 : 55)));
      state = 0;
    }
  }
break;}
default: {
  throw new Exception("CssEncoder");
}
}
}
switch (state){
case (0): {
break;}
case (4): {
  throw new Exception("CssEncoder");
break;}
case (3): {
  throw new Exception("CssEncoder");
break;}
case (2): {
  throw new Exception("CssEncoder");
break;}
case (1): {
  throw new Exception("CssEncoder");
break;}
default: {
  throw new Exception("CssEncoder");
}
}
return output.ToString();
}

public static IEnumerable<char> Transduce(IEnumerable<char> input){
var encoder = new CssEncoder();
int state = encoder.q0;
foreach (char c in input)
{
  foreach (char d in encoder.Psi(state, (int)c))
    yield return d;
  state = encoder.Delta(state, (int)c);
}
if (encoder.F.Contains(state))
{
  foreach (char d in encoder.Psi(state, -1))
    yield return d;
}
else
  throw new Exception("CssEncoder");
}


public ICollection<int> Q { get { return new int[]{ 0, 4, 3, 2, 1, -1 }; } }

public int q0 { get { return 0 ; } }

public ICollection<int> F { get { return new int[]{ 0,  }; } }

public IEnumerable<char> Sigma { get { for (char i = char.MinValue; i <= char.MaxValue; i++) yield return i; } }

public int Delta(int state, int c){
if (c < 0) return state;
switch (state){
case (0): {
  if (((c==0xFFFE)||(c==0xFFFF))) {
    state = -1;
  } else {
    if (((0xDC00<=c)&&(c<=0xDFFF))) {
      state = -1;
    } else {
      if (((0xD800<=c)&&(c<=0xDBFF))) {
        if ((Bits(1,0,c)==0)) {
          state = 4;
        } else {
          if ((Bits(1,0,c)==2)) {
            state = 3;
          } else {
            if ((Bits(1,0,c)==3)) {
              state = 2;
            } else {
              state = 1;
            }
          }
        }
      } else {
        if (!(Bits(15,8,c)==0)) {
          state = 0;
        } else {
          if ((((0x30<=c)&&(Bits(15,6,c)==0)&&(Bits(5,0,c)<=0x39))||((0x41<=c)&&(Bits(15,7,c)==0)&&(Bits(6,0,c)<=0x5A))||((0x61<=c)&&(Bits(15,7,c)==0)&&(Bits(6,0,c)<=0x7A))||((0x80<=c)&&(Bits(15,8,c)==0)&&(Bits(7,0,c)<=0x90))||((0x93<=c)&&(Bits(15,8,c)==0)&&(Bits(7,0,c)<=0x9A))||((0xA0<=c)&&(Bits(15,8,c)==0)&&(Bits(7,0,c)<=0xA5)))) {
            state = 0;
          } else {
            state = 0;
          }
        }
      }
    }
  }
break;}
case (4): {
  if (((c==0xFFFE)||(c==0xFFFF))) {
    state = -1;
  } else {
    if (((0xDC00>c)||(c>0xDFFF))) {
      state = -1;
    } else {
      state = 0;
    }
  }
break;}
case (3): {
  if (((c==0xFFFE)||(c==0xFFFF))) {
    state = -1;
  } else {
    if (((0xDC00>c)||(c>0xDFFF))) {
      state = -1;
    } else {
      state = 0;
    }
  }
break;}
case (2): {
  if (((c==0xFFFE)||(c==0xFFFF))) {
    state = -1;
  } else {
    if (((0xDC00>c)||(c>0xDFFF))) {
      state = -1;
    } else {
      state = 0;
    }
  }
break;}
case (1): {
  if (((c==0xFFFE)||(c==0xFFFF))) {
    state = -1;
  } else {
    if (((0xDC00>c)||(c>0xDFFF))) {
      state = -1;
    } else {
      state = 0;
    }
  }
break;}
default: {
state = -1;
break;
}
}
return state;
}

public IEnumerable<char> Psi(int state, int c){
if (c >= 0) {
switch (state){
case (0): {
  if (((c==0xFFFE)||(c==0xFFFF))) {
    yield break;
  } else {
    if (((0xDC00<=c)&&(c<=0xDFFF))) {
      yield break;
    } else {
      if (((0xD800<=c)&&(c<=0xDBFF))) {
        if ((Bits(1,0,c)==0)) {
          yield return ((char)0x5C); 
          yield return ((char)(Bits(9,6,c)==0xF ? 0x31 : 0x30)); 
          yield return ((char)(((1+Bits(15,6,c))&0xF)+(((1+Bits(15,6,c))&0xF)<=9 ? 48 : 55))); 
          yield return ((char)((Bits(15,2,c)&0xF)+((Bits(15,2,c)&0xF)<=9 ? 48 : 55))); 
        } else {
          if ((Bits(1,0,c)==2)) {
            yield return ((char)0x5C); 
            yield return ((char)(Bits(9,6,c)==0xF ? 0x31 : 0x30)); 
            yield return ((char)(((1+Bits(15,6,c))&0xF)+(((1+Bits(15,6,c))&0xF)<=9 ? 48 : 55))); 
            yield return ((char)((Bits(15,2,c)&0xF)+((Bits(15,2,c)&0xF)<=9 ? 48 : 55))); 
          } else {
            if ((Bits(1,0,c)==3)) {
              yield return ((char)0x5C); 
              yield return ((char)(Bits(9,6,c)==0xF ? 0x31 : 0x30)); 
              yield return ((char)(((1+Bits(15,6,c))&0xF)+(((1+Bits(15,6,c))&0xF)<=9 ? 48 : 55))); 
              yield return ((char)((Bits(15,2,c)&0xF)+((Bits(15,2,c)&0xF)<=9 ? 48 : 55))); 
            } else {
              yield return ((char)0x5C); 
              yield return ((char)(Bits(9,6,c)==0xF ? 0x31 : 0x30)); 
              yield return ((char)(((1+Bits(15,6,c))&0xF)+(((1+Bits(15,6,c))&0xF)<=9 ? 48 : 55))); 
              yield return ((char)((Bits(15,2,c)&0xF)+((Bits(15,2,c)&0xF)<=9 ? 48 : 55))); 
            }
          }
        }
      } else {
        if (!(Bits(15,8,c)==0)) {
          yield return ((char)0x5C); 
          yield return ((char)0x30); 
          yield return ((char)0x30); 
          yield return ((char)(((c>>12)&0xF)+(((c>>12)&0xF)<=9 ? 48 : 55))); 
          yield return ((char)(((c>>8)&0xF)+(((c>>8)&0xF)<=9 ? 48 : 55))); 
          yield return ((char)(((c>>4)&0xF)+(((c>>4)&0xF)<=9 ? 48 : 55))); 
          yield return ((char)((c&0xF)+((c&0xF)<=9 ? 48 : 55))); 
        } else {
          if ((((0x30<=c)&&(Bits(15,6,c)==0)&&(Bits(5,0,c)<=0x39))||((0x41<=c)&&(Bits(15,7,c)==0)&&(Bits(6,0,c)<=0x5A))||((0x61<=c)&&(Bits(15,7,c)==0)&&(Bits(6,0,c)<=0x7A))||((0x80<=c)&&(Bits(15,8,c)==0)&&(Bits(7,0,c)<=0x90))||((0x93<=c)&&(Bits(15,8,c)==0)&&(Bits(7,0,c)<=0x9A))||((0xA0<=c)&&(Bits(15,8,c)==0)&&(Bits(7,0,c)<=0xA5)))) {
            yield return ((char)c); 
          } else {
            yield return ((char)0x5C); 
            yield return ((char)0x30); 
            yield return ((char)0x30); 
            yield return ((char)0x30); 
            yield return ((char)0x30); 
            yield return ((char)(((c>>4)&0xF)+(((c>>4)&0xF)<=9 ? 48 : 55))); 
            yield return ((char)((c&0xF)+((c&0xF)<=9 ? 48 : 55))); 
          }
        }
      }
    }
  }
break;}
case (4): {
  if (((c==0xFFFE)||(c==0xFFFF))) {
    yield break;
  } else {
    if (((0xDC00>c)||(c>0xDFFF))) {
      yield break;
    } else {
      yield return ((char)((Bits(9,8,c)&0xF)+((Bits(9,8,c)&0xF)<=9 ? 48 : 55))); 
      yield return ((char)(((c>>4)&0xF)+(((c>>4)&0xF)<=9 ? 48 : 55))); 
      yield return ((char)((c&0xF)+((c&0xF)<=9 ? 48 : 55))); 
    }
  }
break;}
case (3): {
  if (((c==0xFFFE)||(c==0xFFFF))) {
    yield break;
  } else {
    if (((0xDC00>c)||(c>0xDFFF))) {
      yield break;
    } else {
      yield return ((char)(((8|Bits(9,8,c))&0xF)+(((8|Bits(9,8,c))&0xF)<=9 ? 48 : 55))); 
      yield return ((char)(((c>>4)&0xF)+(((c>>4)&0xF)<=9 ? 48 : 55))); 
      yield return ((char)((c&0xF)+((c&0xF)<=9 ? 48 : 55))); 
    }
  }
break;}
case (2): {
  if (((c==0xFFFE)||(c==0xFFFF))) {
    yield break;
  } else {
    if (((0xDC00>c)||(c>0xDFFF))) {
      yield break;
    } else {
      yield return ((char)(((0xC|Bits(9,8,c))&0xF)+(((0xC|Bits(9,8,c))&0xF)<=9 ? 48 : 55))); 
      yield return ((char)(((c>>4)&0xF)+(((c>>4)&0xF)<=9 ? 48 : 55))); 
      yield return ((char)((c&0xF)+((c&0xF)<=9 ? 48 : 55))); 
    }
  }
break;}
case (1): {
  if (((c==0xFFFE)||(c==0xFFFF))) {
    yield break;
  } else {
    if (((0xDC00>c)||(c>0xDFFF))) {
      yield break;
    } else {
      yield return ((char)(((4|Bits(9,8,c))&0xF)+(((4|Bits(9,8,c))&0xF)<=9 ? 48 : 55))); 
      yield return ((char)(((c>>4)&0xF)+(((c>>4)&0xF)<=9 ? 48 : 55))); 
      yield return ((char)((c&0xF)+((c&0xF)<=9 ? 48 : 55))); 
    }
  }
break;}
default: {
yield break;
}
}
}
else {
switch (state){
case (0): {
break;}
case (4): {
  yield break;
break;}
case (3): {
  yield break;
break;}
case (2): {
  yield break;
break;}
case (1): {
  yield break;
break;}
default: {
yield break;
}
}
}
}
}
}
