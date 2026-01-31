/**
 * Weather classification enum
 */
export enum WeatherClassification {
  Good = 'Good',
  Bad = 'Bad',
  Windy = 'Windy',
  Rainy = 'Rainy',
  Cold = 'Cold',
  Hot = 'Hot'
}

/**
 * Location DTO
 */
export interface Location {
  id: number;
  name: string;
  latitude: number;
  longitude: number;
}

/**
 * Weather data DTO
 */
export interface Weather {
  temperatureC: number;
  precipitationMm: number;
  windKph: number;
  conditionText: string;
  timestamp: string;
  locationName: string;
  latitude: number;
  longitude: number;
}

/**
 * Activity recommendation DTO
 */
export interface ActivityRecommendation {
  name: string;
  description: string;
  isIndoor: boolean;
  type: string;
  explanation?: string;
}

/**
 * Event DTO
 */
export interface Event {
  id: string;
  name: string;
  description?: string;
  startDate: string;
  endDate: string;
  location?: string;
  isIndoor: boolean;
}

/**
 * Point of Interest DTO
 */
export interface Poi {
  id: string;
  name: string;
  type?: string;
  description?: string;
  address?: string;
  isIndoor: boolean;
  latitude?: number;
  longitude?: number;
}

/**
 * Weather response with classification and recommendations
 */
export interface WeatherResponse {
  weather: Weather;
  classifications: WeatherClassification[];
  recommendations: ActivityRecommendation[];
}

/**
 * Full recommendation response including LLM explanation
 */
export interface RecommendationResponse {
  explanation: string;
  weather?: Weather;
  classifications: WeatherClassification[];
  recommendations: ActivityRecommendation[];
  sourceEvents: Event[];
  sourcePois: Poi[];
}

/**
 * Chat request DTO
 */
export interface ChatRequest {
  message: string;
}

/**
 * Chat response DTO
 */
export interface ChatResponse {
  response: string;
  data?: RecommendationResponse;
}

/**
 * Chat message for display
 */
export interface ChatMessage {
  content: string;
  isUser: boolean;
  timestamp: Date;
  data?: RecommendationResponse;
}
