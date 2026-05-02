import { all, create } from "mathjs"
import { Lazy } from "../WebSharper.Core.JavaScript/Runtime.js"
let _c=Lazy((_i) => class $StartupCode_Decimal {
  static {
    _c=_i(this);
  }
  static WSDecimalMath;
  static {
    let r;
    let a=all;
    let b=(r={},r.number="BigNumber",r.precision=29,r.predictable=true,r.epsilon=1E-60,r);
    let _1=create(a, b);
    this.WSDecimalMath=_1;
  }
});
export default _c;
