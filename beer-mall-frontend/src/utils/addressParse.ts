interface AreaItem {
  code: string;
  name: string;
  children?: AreaItem[];
}

export interface AddressResult {
  mobile: string;
  name: string;
  provinceCode: string;
  provinceName: string;
  cityCode: string;
  cityName: string;
  areaCode: string;
  areaName: string;
  detail: string;
}
// ... 前面的接口定义保持不变 ...

export const smartParse = (text: string, areaData: AreaItem[]): AddressResult => {
  const result: AddressResult = {
    mobile: '',
    name: '',
    provinceCode: '',
    provinceName: '',
    cityCode: '',
    cityName: '',
    areaCode: '',
    areaName: '',
    detail: ''
  };

  // 1. 深度清洗
  let parseText = text
    .replace(/收货人|联系人|姓名|手机号|手机|电话|所在地区|地区|详细地址|地址|：|:/g, ' ')
    .replace(/[\r\n,，\t]/g, ' ')
    .trim();

  // 2. 提取手机号
  const mobileReg = /(1\d{10})/g;
  const mobileMatch = parseText.match(mobileReg);
  if (mobileMatch && mobileMatch.length > 0) {
    result.mobile = mobileMatch[0];
    parseText = parseText.replace(result.mobile, ' ').trim();
  }

  // 辅助函数：模糊匹配地名
  const matchArea = (text: string, areaName: string) => {
    if (!areaName) return false;
    if (text.includes(areaName)) return true;
    // 两个字以上的地名，允许只匹配前两个字 (如 "北京" 匹配 "北京市")
    if (areaName.length > 2 && text.includes(areaName.substring(0, 2))) return true;
    return false;
  };

  // 3. 匹配行政区划
  // 3.1 找省份
  const province = areaData.find(p => matchArea(parseText, p.name));
  
  if (province) {
    result.provinceCode = province.code;
    result.provinceName = province.name;
    // 剔除省名
    parseText = parseText.replace(province.name, ' ').replace(province.name.substring(0, 2), ' ').trim();

    // 判断是否为直辖市 (北京11, 天津12, 上海31, 重庆50)
    const isDirectCity = ['11', '12', '31', '50'].includes(province.code.substring(0, 2));

    if (province.children && province.children.length > 0) {
      
      // 3.2 找城市 (City)
      let city = province.children.find(c => matchArea(parseText, c.name));

      // 【核心修改】针对直辖市的特殊逻辑
      if (isDirectCity) {
          // 直辖市逻辑：
          // 1. 如果文本里真的有 "市辖区" (极少见)，那就填上。
          // 2. 如果文本里没有，保持 city 为空，不要强制填充。
          // 3. 直接在所有二级子节点（市辖区、县）的 children 里找区县。
          
          if (city) {
            // 如果用户真的输入了 "市辖区"，则记录
             result.cityCode = city.code;
             result.cityName = city.name;
             parseText = parseText.replace(city.name, ' ').trim();
          }

          // 寻找区县 (District)：遍历该省下所有二级节点（市辖区、县）的子节点
          // 如果前面找到了 city，就只在那个 city 下找；否则遍历所有 city 找
          const citySearchList = city ? [city] : province.children;
          
          for (const childCity of citySearchList) {
              if (childCity.children) {
                  const area = childCity.children.find(a => parseText.includes(a.name));
                  if (area) {
                      result.areaCode = area.code;
                      result.areaName = area.name;
                      parseText = parseText.replace(area.name, ' ').trim();
                      
                      // 找到了区县，如果此时 city 还是空的，就让它保持空
                      // (这就实现了“直辖市的市辖区允许为空”)
                      break; 
                  }
              }
          }

      } else {
          // 【普通省份逻辑】 (如 河北 -> 石家庄 -> 长安区)
          if (city) {
             result.cityCode = city.code;
             result.cityName = city.name;
             parseText = parseText.replace(city.name, ' ').replace(city.name.substring(0, 2), ' ').trim();

             // 3.3 找区县
             if (city.children && city.children.length > 0) {
                 const area = city.children.find(a => parseText.includes(a.name));
                 if (area) {
                     result.areaCode = area.code;
                     result.areaName = area.name;
                     parseText = parseText.replace(area.name, ' ').trim();
                 }
             }
          }
      }
    }
  }

  // 4. 区分姓名与详细地址 (保持之前的增强逻辑)
  const fragments = parseText.split(/\s+/).filter(s => s.length > 0);
  const addressKeywords = ['路', '街', '号', '室', '苑', '栋', '单元', '大厦', '村', '镇', '弄', '巷', '区', '楼', '城', '园', '座'];
  
  let detailArr: string[] = [];

  fragments.forEach(frag => {
    if (result.name) {
      detailArr.push(frag);
      return;
    }
    const isEnglishName = /^[a-zA-Z]+$/.test(frag) && frag.length < 15;
    const isChineseName = /^[\u4e00-\u9fa5]{2,4}$/.test(frag);
    const hasAddressKey = addressKeywords.some(key => frag.includes(key));

    if (!hasAddressKey && (isEnglishName || isChineseName)) {
      result.name = frag;
    } else {
      detailArr.push(frag);
    }
  });

  // 兜底策略：如果没名字且片段短，强行认作名字
  if (!result.name && detailArr.length > 0) {
      const candidateIndex = detailArr.findIndex(f => f.length < 5 && !/\d/.test(f));
      if (candidateIndex > -1) {
          result.name = detailArr[candidateIndex];
          detailArr.splice(candidateIndex, 1);
      }
  }

  result.detail = detailArr.join('');
  return result;
};