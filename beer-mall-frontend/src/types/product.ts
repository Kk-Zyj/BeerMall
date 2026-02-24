// src/types/product.ts

// 这对应你 C# 后端的 ProductListDto
export interface Product {
  id: number;
  categoryId: number; 
  name: string;
  mainImage: string; // 注意：后端如果是 PascalCase，JSON 序列化默认会转为 camelCase
  minPrice: number;
  sales: number;
  description: string;
  skuCount: number;
  defaultSkuId: number;
}

// SKU 模型 (对应 C# 的 ProductSku)
export interface Sku {
  id: number;
  specName: string; // "单瓶500ml" 或 "整箱24瓶"
  price: number;    // 重点：不同规格价格不同
  originalPrice: number;
  stock: number;
  weight: number;
}

// 商品详情模型 (对应 C# 的 ProductDetailDto)
export interface ProductDetail {
  id: number;
  name: string;
  description: string;
  mainImage: string;
  content: string; // 富文本详情
  skus: Sku[];     // 重点：包含所有规格列表
}