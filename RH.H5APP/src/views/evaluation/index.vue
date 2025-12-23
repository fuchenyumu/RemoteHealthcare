<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { showToast, showSuccessToast } from 'vant'
import { submitEvaluation, getConsultation } from '@/api/evaluation'

const props = defineProps<{
  id: string
}>()

const loading = ref(false)
const consultation = ref<any>(null)
const score = ref(5)
const content = ref('')
const selectedTags = ref([])
const isAnonymous = ref(false)
const submitted = ref(false)

const tagOptions = ['态度好', '专业性强', '解答清晰', '建议实用', '响应及时', '系统好用']

const loadConsultation = async () => {
  if (props.id === '0') return
  try {
    const res = await getConsultation(props.id)
    consultation.value = res.data
  } catch (error) {
    showToast('获取会诊信息失败')
  }
}

const onSubmit = async () => {
  if (!props.id || props.id === '0') {
    showToast('无效的会诊ID')
    return
  }
  
  loading.value = true
  try {
    await submitEvaluation({
      consultationId: parseInt(props.id),
      patientName: consultation.value?.patientName || '患者',
      score: score.value,
      tags: selectedTags.value.join(','),
      content: content.value,
      isAnonymous: isAnonymous.value
    })
    showSuccessToast('评价提交成功')
    submitted.value = true
  } catch (error) {
    // Error handled by interceptor
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  loadConsultation()
})
</script>

<template>
  <div class="evaluation-page">
    <van-nav-bar title="会诊评价" />
    
    <div v-if="!submitted" class="form-container">
      <div class="header" v-if="consultation">
        <h3>{{ consultation.patientName }} 的会诊评价</h3>
        <p>编号：{{ consultation.id }}</p>
      </div>

      <van-cell-group inset title="总体评价">
        <van-cell title="满意度评分" center>
          <template #right-icon>
            <van-rate v-model="score" color="#ffd21e" void-icon="star" void-color="#eee" />
          </template>
        </van-cell>
      </van-cell-group>

      <van-cell-group inset title="评价标签">
        <div class="tag-group">
          <van-checkbox-group v-model="selectedTags" direction="horizontal">
            <van-checkbox 
              v-for="tag in tagOptions" 
              :key="tag" 
              :name="tag"
              shape="square"
              class="tag-item"
            >
              {{ tag }}
            </van-checkbox>
          </van-checkbox-group>
        </div>
      </van-cell-group>

      <van-cell-group inset title="意见建议">
        <van-field
          v-model="content"
          rows="4"
          autosize
          type="textarea"
          placeholder="请输入您的宝贵意见..."
          maxlength="200"
          show-word-limit
        />
      </van-cell-group>

      <div class="anonymous-wrap">
        <van-checkbox v-model="isAnonymous">匿名评价</van-checkbox>
      </div>

      <div class="submit-wrap">
        <van-button 
          round 
          block 
          type="primary" 
          :loading="loading" 
          @click="onSubmit"
        >
          提交评价
        </van-button>
      </div>
    </div>

    <div v-else class="result-container">
      <div class="success-icon">
        <van-icon name="passed" color="#07c160" size="80" />
      </div>
      <h2>提交成功</h2>
      <p>感谢您的参与，我们将持续改进服务！</p>
      <div class="close-btn">
        <van-button round block type="default" @click="() => submitted = false">
          再写一单（演示）
        </van-button>
      </div>
    </div>
  </div>
</template>

<style scoped>
.evaluation-page {
  min-height: 100vh;
  padding-bottom: 40px;
}

.form-container {
  padding: 16px 0;
}

.header {
  padding: 0 16px 16px;
}

.header h3 {
  margin: 0;
  color: #323233;
}

.header p {
  margin: 4px 0 0;
  font-size: 14px;
  color: #969799;
}

.tag-group {
  padding: 12px 16px;
}

.tag-item {
  margin-bottom: 8px;
  margin-right: 8px;
}

.anonymous-wrap {
  padding: 16px;
  display: flex;
  justify-content: flex-end;
}

.submit-wrap {
  margin: 16px;
}

.result-container {
  display: flex;
  flex-direction: column;
  align-items: center;
  padding-top: 60px;
}

.success-icon {
  margin-bottom: 24px;
}

.result-container h2 {
  margin: 0 0 8px;
  color: #323233;
}

.result-container p {
  color: #969799;
  font-size: 14px;
}

.close-btn {
  margin-top: 40px;
  width: 80%;
}
</style>

