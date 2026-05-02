import { WSDecimalMath } from "./WebSharper.Decimal.js"
export function DecodeJson(n){
  return WSDecimalMath().bignumber(n);
}
export function EncodeJson(n){
  return WSDecimalMath().string(n);
}
