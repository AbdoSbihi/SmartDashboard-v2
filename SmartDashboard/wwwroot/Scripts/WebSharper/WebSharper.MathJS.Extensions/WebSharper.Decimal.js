import { length, get } from "../WebSharper.StdLib/Microsoft.FSharp.Core.LanguagePrimitives.IntrinsicFunctions.js"
import { InvalidArg, toInt } from "../WebSharper.StdLib/Microsoft.FSharp.Core.Operators.js"
import $StartupCode_Decimal from "./$StartupCode_Decimal.js"
export function CreateDecimalBits(bits){
  return length(bits)===4?CreateDecimal(get(bits, 0), get(bits, 1), get(bits, 2), (get(bits, 3)&-2147483648)!==0, get(bits, 3)>>16&127):InvalidArg("bits", "The length of the bits array is not 4");
}
export function CreateDecimal(lo, mid, hi, isNegative, scale){
  const n=(x) => WSDecimalMath().bignumber(x);
  if(lo===0&&hi===0&&mid===0)return n(0);
  else {
    const a=n(429496729);
    const b=n(10);
    const a_1=WSDecimalMath().multiply(a, b);
    const b_1=n(6);
    const uint_sup=WSDecimalMath().add(a_1, b_1);
    const reinterpret=(x) => {
      if(x>=0)return n(x);
      else {
        const a_8=uint_sup;
        const b_8=n(x);
        return WSDecimalMath().add(a_8, b_8);
      }
    };
    const quotient=WSDecimalMath().pow(n(10), WSDecimalMath().unaryMinus(n(toInt(scale))));
    const a_2=reinterpret(hi);
    const b_2=uint_sup;
    const a_3=WSDecimalMath().multiply(a_2, b_2);
    const b_3=reinterpret(mid);
    const a_4=WSDecimalMath().add(a_3, b_3);
    const b_4=uint_sup;
    const a_5=WSDecimalMath().multiply(a_4, b_4);
    const b_5=reinterpret(lo);
    const value=WSDecimalMath().add(a_5, b_5);
    const a_6=isNegative?n(-1):n(1);
    const b_6=value;
    const a_7=WSDecimalMath().multiply(a_6, b_6);
    const b_7=quotient;
    return WSDecimalMath().multiply(a_7, b_7);
  }
}
export function WSDecimalMath(){
  return $StartupCode_Decimal.WSDecimalMath;
}
