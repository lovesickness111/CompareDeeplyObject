/**
 * Compare every difference between 2 object
 * @param currentObject: random object or array like {"name": "Cuong", "age": 26}
 * @param expectObject: random object or array like {"name": "Viet"}
 * @param ignoreKeys: Array of ignore compare keys
 * @returns: ex: {"name": {"current":"Cuong", "expect":"Viet"}, "age": {"current":26, "expect": null}} 
 */
export function compareObjects(currentObject: any, expectObject: any, ignoreKeys: string[] = [], ignoreFalsy = false, ignoreArrayEmpty = false, isIgnoreType = false) {
  try {
    const differences = {} as any;

    // tqcong: check null cho param
    if (!ignoreFalsy && (!currentObject || !expectObject)){
      throw new Error('currentObject hoặc expectObject có giá trị null hoặc undefined');
    }
    // nếu truyền vào đã là so sánh object với null
    if ((!currentObject || !expectObject) && currentObject != expectObject) {

      differences["root"] = {
        current: currentObject,
        expect: expectObject,
      };
      return differences;
    }

    function compareProps(prop: string, path: any) {
      /** nếu gặp key không cần so sánh thì bỏ qua */
      if (ignoreKeys.includes(prop)) {
        return;
      }
      if (typeof currentObject[prop] === 'object' && typeof expectObject[prop] === 'object') {
        // nếu gặp object. tiếp tục đệ quy
        const nestedDifferences = compareObjects(
          currentObject[prop],
          expectObject[prop],
          ignoreKeys,
          ignoreFalsy,
          ignoreArrayEmpty,
          isIgnoreType
        );
        if (Object.keys(nestedDifferences).length > 0) {
          differences[path] = nestedDifferences;
        }
      } else if (currentObject[prop] !== expectObject[prop]) {
        // nếu không phải object, chỉ cần so sánh value và kiểu của 2 object
        if(ignoreFalsy && !currentObject[prop]){
          currentObject[prop] = undefined;
        }
        if(ignoreFalsy && !expectObject[prop]){
          expectObject[prop] = undefined;
        }
        if(ignoreArrayEmpty && Array.isArray(currentObject[prop]) && currentObject[prop].length == 0){
          currentObject[prop] = undefined;
        }
        if(ignoreArrayEmpty && Array.isArray(expectObject[prop]) && expectObject[prop].length == 0){
          expectObject[prop] = undefined;
        }
        // nếu KHÔNG so sánh kiểu (isIgnoreType = true) thì 100 = "100" / nếu PHẢI so sánh kiểu (isIgnoreType = false) thì 100 != "100"
        if((!isIgnoreType && currentObject[prop] !== expectObject[prop]) || (isIgnoreType && currentObject[prop] != expectObject[prop])){
          differences[path] = {
            current: currentObject[prop],
            expect: expectObject[prop],
          };
        }

      }
    }

    for (let prop in currentObject) {
      if (currentObject.hasOwnProperty(prop)) {
        compareProps(prop, prop);
      }
    }

    for (let prop in expectObject) {
      if (expectObject.hasOwnProperty(prop) && currentObject && !currentObject.hasOwnProperty(prop)) {
        compareProps(prop, prop);
      }
    }

    return differences;
  } catch (error) {
    console.log(error);
  }
}
