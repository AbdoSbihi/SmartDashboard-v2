import { Get } from "./WebSharper.Enumerator.js"
import { isIDisposable } from "./System.IDisposable.js"
export function GetJS(x, items){
  let x_1, _1;
  x_1=x;
  const e=Get(items);
  try {
    while(e.MoveNext())
      {
        const i=e.Current;
        x_1=x_1[i];
      }
    _1=void 0;
  }
  finally {
    const _2=e;
    if(typeof _2=="object"&&isIDisposable(_2))e.Dispose();
  }
  return x_1;
}
export function NewFromSeq(fields){
  let _1;
  const r={};
  const e=Get(fields);
  try {
    while(e.MoveNext())
      {
        const f=e.Current;
        r[f[0]]=f[1];
      }
    _1=void 0;
  }
  finally {
    const _2=e;
    if(typeof _2=="object"&&isIDisposable(_2))e.Dispose();
  }
  return r;
}
