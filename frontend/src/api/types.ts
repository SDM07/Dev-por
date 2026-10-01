export type HttpMethod = 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE';
export type ApiStatus = 'STABLE' | 'BETA' | 'DEPRECATED';

export interface Technology {
  slug: string;
  name: string;
  color: string;
  apiCount: number;
}

export interface ApiSummary {
  slug: string;
  name: string;
  summary: string;
  technology: string;
  technologyName: string;
  category: string;
  version: string;
  ownerTeam: string;
  status: ApiStatus;
  tags: string[];
  updatedAt: string;
}

export interface Endpoint {
  id: string;
  method: HttpMethod;
  path: string;
  title: string;
  descriptionMd: string;
  requestExample?: string;
  responseExample?: string;
}

export interface ApiDetail extends ApiSummary {
  descriptionMd: string;
  baseUrl: string;
  endpoints: Endpoint[];
}

export interface ApiFilters {
  technologies: string[];
  q: string;
  category?: string;
}

export type ApiUpsert = Omit<ApiDetail, 'technologyName' | 'updatedAt'>;
