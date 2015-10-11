using System;
using System.Text;
using System.Collections.Generic;

namespace Microsoft.Bek.Tests {
public class UTF8Encoder : Microsoft.Automata.IDeterministicFiniteTransducer
{
static int Bits(int m, int n, int c){return ((c >> n) & ~(-2 << (m - n)));}

public static string Apply(string input){
var output = new StringBuilder(input.Length >= 16384 ? input.Length : (int)(Math.Min(16384, (long)input.Length * 3)));
int state = 0;
for (int i = 0; i < input.Length; i++){
int c = (int)input[i];
switch (state){
case (0): {
  if ((Bits(15,7,c)==0)) {
    output.Append((char)c);
    state = 0;
  } else {
    if ((!(Bits(15,7,c)==0)&&(Bits(15,11,c)==0))) {
      output.Append((char)(0xC0|Bits(10,6,c)));
      output.Append((char)(0x80|Bits(5,0,c)));
      state = 0;
    } else {
      if ((!(Bits(15,11,c)==0)&&((0xD800>c)||(c>0xDFFF)))) {
        output.Append((char)(0xE0|Bits(15,12,c)));
        output.Append((char)(0x80|Bits(11,6,c)));
        output.Append((char)(0x80|Bits(5,0,c)));
        state = 0;
      } else {
        if (((0xD800<=c)&&(c<=0xDBFF))) {
          if ((Bits(1,0,c)==0)) {
            output.Append((char)(0xF0|Bits(4,2,(1+Bits(9,6,c)))));
            output.Append((char)(((8|((1+Bits(7,6,c))&3))<<4)|Bits(5,2,c)));
            state = 4;
          } else {
            if ((Bits(1,0,c)==2)) {
              output.Append((char)(0xF0|Bits(4,2,(1+Bits(9,6,c)))));
              output.Append((char)(((8|((1+Bits(7,6,c))&3))<<4)|Bits(5,2,c)));
              state = 3;
            } else {
              if ((Bits(1,0,c)==3)) {
                output.Append((char)(0xF0|Bits(4,2,(1+Bits(9,6,c)))));
                output.Append((char)(((8|((1+Bits(7,6,c))&3))<<4)|Bits(5,2,c)));
                state = 2;
              } else {
                output.Append((char)(0xF0|Bits(4,2,(1+Bits(9,6,c)))));
                output.Append((char)(((8|((1+Bits(7,6,c))&3))<<4)|Bits(5,2,c)));
                state = 1;
              }
            }
          }
        } else {
          throw new Exception("UTF8Encoder");
        }
      }
    }
  }
break;}
case (4): {
  if (((0xDC00<=c)&&(c<=0xDFFF))) {
    output.Append((char)(0x80|Bits(9,6,c)));
    output.Append((char)(0x80|Bits(5,0,c)));
    state = 0;
  } else {
    throw new Exception("UTF8Encoder");
  }
break;}
case (3): {
  if (((0xDC00<=c)&&(c<=0xDFFF))) {
    output.Append((char)(0xA0|Bits(9,6,c)));
    output.Append((char)(0x80|Bits(5,0,c)));
    state = 0;
  } else {
    throw new Exception("UTF8Encoder");
  }
break;}
case (2): {
  if (((0xDC00<=c)&&(c<=0xDFFF))) {
    output.Append((char)(0xB0|Bits(9,6,c)));
    output.Append((char)(0x80|Bits(5,0,c)));
    state = 0;
  } else {
    throw new Exception("UTF8Encoder");
  }
break;}
case (1): {
  if (((0xDC00<=c)&&(c<=0xDFFF))) {
    output.Append((char)(0x90|Bits(9,6,c)));
    output.Append((char)(0x80|Bits(5,0,c)));
    state = 0;
  } else {
    throw new Exception("UTF8Encoder");
  }
break;}
default: {
  throw new Exception("UTF8Encoder");
}
}
}
switch (state){
case (0): {
break;}
case (4): {
  throw new Exception("UTF8Encoder");
break;}
case (3): {
  throw new Exception("UTF8Encoder");
break;}
case (2): {
  throw new Exception("UTF8Encoder");
break;}
case (1): {
  throw new Exception("UTF8Encoder");
break;}
default: {
  throw new Exception("UTF8Encoder");
}
}
return output.ToString();
}

public static IEnumerable<char> Transduce(IEnumerable<char> input){
var encoder = new UTF8Encoder();
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
  throw new Exception("UTF8Encoder");
}


public ICollection<int> Q { get { return new int[]{ 0, 4, 3, 2, 1, -1 }; } }

public int q0 { get { return 0 ; } }

public ICollection<int> F { get { return new int[]{ 0,  }; } }

public IEnumerable<char> Sigma { get { for (char i = char.MinValue; i <= char.MaxValue; i++) yield return i; } }

public int Delta(int state, int c){
if (c < 0) return state;
switch (state){
case (0): {
  if ((Bits(15,7,c)==0)) {
    state = 0;
  } else {
    if ((!(Bits(15,7,c)==0)&&(Bits(15,11,c)==0))) {
      state = 0;
    } else {
      if ((!(Bits(15,11,c)==0)&&((0xD800>c)||(c>0xDFFF)))) {
        state = 0;
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
          state = -1;
        }
      }
    }
  }
break;}
case (4): {
  if (((0xDC00<=c)&&(c<=0xDFFF))) {
    state = 0;
  } else {
    state = -1;
  }
break;}
case (3): {
  if (((0xDC00<=c)&&(c<=0xDFFF))) {
    state = 0;
  } else {
    state = -1;
  }
break;}
case (2): {
  if (((0xDC00<=c)&&(c<=0xDFFF))) {
    state = 0;
  } else {
    state = -1;
  }
break;}
case (1): {
  if (((0xDC00<=c)&&(c<=0xDFFF))) {
    state = 0;
  } else {
    state = -1;
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
  if ((Bits(15,7,c)==0)) {
    yield return ((char)c); 
  } else {
    if ((!(Bits(15,7,c)==0)&&(Bits(15,11,c)==0))) {
      yield return ((char)(0xC0|Bits(10,6,c))); 
      yield return ((char)(0x80|Bits(5,0,c))); 
    } else {
      if ((!(Bits(15,11,c)==0)&&((0xD800>c)||(c>0xDFFF)))) {
        yield return ((char)(0xE0|Bits(15,12,c))); 
        yield return ((char)(0x80|Bits(11,6,c))); 
        yield return ((char)(0x80|Bits(5,0,c))); 
      } else {
        if (((0xD800<=c)&&(c<=0xDBFF))) {
          if ((Bits(1,0,c)==0)) {
            yield return ((char)(0xF0|Bits(4,2,(1+Bits(9,6,c))))); 
            yield return ((char)(((8|((1+Bits(7,6,c))&3))<<4)|Bits(5,2,c))); 
          } else {
            if ((Bits(1,0,c)==2)) {
              yield return ((char)(0xF0|Bits(4,2,(1+Bits(9,6,c))))); 
              yield return ((char)(((8|((1+Bits(7,6,c))&3))<<4)|Bits(5,2,c))); 
            } else {
              if ((Bits(1,0,c)==3)) {
                yield return ((char)(0xF0|Bits(4,2,(1+Bits(9,6,c))))); 
                yield return ((char)(((8|((1+Bits(7,6,c))&3))<<4)|Bits(5,2,c))); 
              } else {
                yield return ((char)(0xF0|Bits(4,2,(1+Bits(9,6,c))))); 
                yield return ((char)(((8|((1+Bits(7,6,c))&3))<<4)|Bits(5,2,c))); 
              }
            }
          }
        } else {
          yield break;
        }
      }
    }
  }
break;}
case (4): {
  if (((0xDC00<=c)&&(c<=0xDFFF))) {
    yield return ((char)(0x80|Bits(9,6,c))); 
    yield return ((char)(0x80|Bits(5,0,c))); 
  } else {
    yield break;
  }
break;}
case (3): {
  if (((0xDC00<=c)&&(c<=0xDFFF))) {
    yield return ((char)(0xA0|Bits(9,6,c))); 
    yield return ((char)(0x80|Bits(5,0,c))); 
  } else {
    yield break;
  }
break;}
case (2): {
  if (((0xDC00<=c)&&(c<=0xDFFF))) {
    yield return ((char)(0xB0|Bits(9,6,c))); 
    yield return ((char)(0x80|Bits(5,0,c))); 
  } else {
    yield break;
  }
break;}
case (1): {
  if (((0xDC00<=c)&&(c<=0xDFFF))) {
    yield return ((char)(0x90|Bits(9,6,c))); 
    yield return ((char)(0x80|Bits(5,0,c))); 
  } else {
    yield break;
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
